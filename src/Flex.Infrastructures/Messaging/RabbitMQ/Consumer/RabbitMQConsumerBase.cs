using Flex.Domain.Entities;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.RabbitMQ.Consumer
{
    /// <summary>
    /// Base class for RabbitMQ consumers implementing the Inbox Pattern for idempotency.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to consume.</typeparam>
    public abstract class RabbitMQConsumerBase<TMessage> : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly RabbitMQOptions _options;
        private IModel? _channel;

        protected RabbitMQConsumerBase(
            IConnection connection, // Use singleton connection
            IOptions<RabbitMQOptions> options,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            _connection = connection;
            _options = options.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected abstract string QueueName { get; }
        protected abstract ushort PrefetchCount { get; }
        protected virtual bool IsDurable => true;

        protected abstract Task HandleMessageAsync(TMessage message, IServiceProvider scopeProvider, CancellationToken cancellationToken);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Create a channel for this consumer
                _channel = _connection.CreateModel();
                
                // Set QoS to control concurrency
                _channel.BasicQos(0, PrefetchCount, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (sender, args) => await OnMessageReceivedAsync(sender, args, stoppingToken);

                _logger.LogInformation("Starting consumer for queue {QueueName}", QueueName);

                _channel.BasicConsume(
                    queue: QueueName,
                    autoAck: false, // Manual Ack for reliability
                    consumer: consumer);

                // Keep the task alive until cancellation
                var completionSource = new TaskCompletionSource();
                stoppingToken.Register(() => completionSource.SetResult());
                return completionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start consumer for queue {QueueName}", QueueName);
                return Task.CompletedTask;
            }
        }

        private async Task OnMessageReceivedAsync(object? sender, BasicDeliverEventArgs args, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                // 1. Idempotency Check
                var messageId = GetIdempotencyKey(args.BasicProperties);
                if (messageId == Guid.Empty)
                {
                    _logger.LogWarning("Message received without valid Idempotency Key (MessageId). RoutingKey: {RoutingKey}. Recommending dead-letter placement.", args.RoutingKey);
                    // Reject without requeue -> DLQ
                    _channel?.BasicNack(args.DeliveryTag, false, false); 
                    return;
                }

                var consumerName = this.GetType().Name;
                var alreadyProcessed = await dbContext.InboxMessages.AnyAsync(
                    x => x.IdempotencyKey == messageId && x.Consumer == consumerName, 
                    stoppingToken);

                if (alreadyProcessed)
                {
                    _logger.LogInformation("Message {MessageId} already processed by {Consumer}. Skipping...", messageId, consumerName);
                    _channel?.BasicAck(args.DeliveryTag, false);
                    return;
                }

                // 2. Deserialize Message
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<TMessage>(json);

                if (message == null)
                {
                    _logger.LogError("Failed to deserialize message {MessageId}. RoutingKey: {RoutingKey}", messageId, args.RoutingKey);
                    _channel?.BasicNack(args.DeliveryTag, false, false); // DLQ
                    return;
                }

                // 3. Handle Message & Save Inbox
                // We do everything in a transaction to ensure atomicity (consume + inbox save)
                // Note: If using multiple resources (e.g. Inbox in SQL, Event in Redis), we need stricter handling.
                // Here we assume Inbox and Business Data are in the same DB Context (IdentityDbContext).
                
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);
                    try
                    {
                        // Business Logic
                        await HandleMessageAsync(message, scope.ServiceProvider, stoppingToken);

                        // Save to Inbox
                        dbContext.InboxMessages.Add(new InboxMessage
                        {
                            IdempotencyKey = messageId,
                            Consumer = consumerName,
                            ProcessedAt = DateTime.UtcNow,
                            Payload = json, // Optional: store payload
                            OccurredAt = GetTimestamp(args.BasicProperties)
                        });

                        await dbContext.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);

                        // 4. Ack RabbitMQ
                        _channel?.BasicAck(args.DeliveryTag, false);
                        _logger.LogInformation("Message {MessageId} processed successfully.", messageId);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        throw; // Re-throw to handle requeue/DLQ logic in the outer catch
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", args.BasicProperties?.MessageId);

                // Retry Strategy
                // Simple: requeue if transient, dead-letter if persistent error.
                // For now, let's requeue transiently? Or Nack to DLQ?
                // Spec says: "Lỗi tạm => BasicNack(requeue:true)", "Lỗi không sửa đươc => BasicNack(requeue:false)".
                
                // Assuming all exceptions here are potentially transient or business failures that need investigation.
                // To avoid infinite loops, we ideally check retry headers (x-death).
                // Simplification for Phase 1: Nack without requeue (to DLQ) for safety, or implement retry count check.
                
                // Let's implement a simple DLQ policy for exceptions to avoid poison pill loops in Phase 1
                _channel?.BasicNack(args.DeliveryTag, false, false); // Send to DLQ
            }
        }

        private Guid GetIdempotencyKey(IBasicProperties properties)
        {
            // Default: Use MessageId if guid, check headers otherwise
            if (Guid.TryParse(properties.MessageId, out var id))
            {
                return id;
            }

            if (properties.Headers != null && 
                properties.Headers.TryGetValue("x-idempotency-key", out var val) && 
                val is byte[] bytes)
            {
                // Assuming it's a byte array or string
                 try 
                 {
                    var str = Encoding.UTF8.GetString(bytes);
                    if (Guid.TryParse(str, out var headerId)) return headerId;
                 }
                 catch { }
            }
            
            return Guid.Empty;
        }

        private DateTime? GetTimestamp(IBasicProperties properties)
        {
            if (properties.IsTimestampPresent())
            {
                return DateTimeOffset.FromUnixTimeSeconds(properties.Timestamp.UnixTime).UtcDateTime;
            }
            return null;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}
