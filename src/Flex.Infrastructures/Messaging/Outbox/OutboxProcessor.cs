using Flex.Domain.Abstractions;
using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Json;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Implementation of IOutboxProcessor that reads pending messages from outbox and publishes them to RabbitMQ.
    /// </summary>
    internal sealed class OutboxProcessor : IOutboxProcessor
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IRabbitMQPublisher _publisher;
        private readonly IEventRoutingResolver _routingResolver;
        private readonly ILogger<OutboxProcessor> _logger;

        private const int MaxRetryCount = 5;
        private const int DefaultBatchSize = 100;

        public OutboxProcessor(
            IdentityDbContext dbContext,
            IRabbitMQPublisher publisher,
            IEventRoutingResolver routingResolver,
            ILogger<OutboxProcessor> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _routingResolver = routingResolver;
            _logger = logger;
        }

        public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            var messages = await this.GetPendingAsync(DefaultBatchSize, cancellationToken);

            if (!messages.Any())
            {
                return;
            }

            _logger.LogInformation("Processing {Count} pending outbox messages", messages.Count);

            foreach (var msg in messages)
            {
                try
                {
                    await this.MarkAsProcessingAsync(msg, cancellationToken);

                    var integrationEvent = await this.DeserializeEventAsync(msg, cancellationToken);
                    
                    // Resolve routing from event type.
                    var routing = _routingResolver.Resolve(integrationEvent.GetType());
                    
                    // Serialize event to JSON and convert to byte[]
                    var jsonPayload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions.Default);
                    var body = Encoding.UTF8.GetBytes(jsonPayload);
                    
                    // Prepare headers.
                    var headers = new Dictionary<string, object>
                    {
                        { "EventType", integrationEvent.GetType().Name },
                        { "OccurredOn", msg.OccurredOn.ToString("O") }
                    };
                    
                    // Publish to RabbitMQ
                    await _publisher.PublishAsync(routing.Exchange, routing.RoutingKey, body, headers, cancellationToken);

                    await this.MarkAsSentAsync(msg, cancellationToken);

                    _logger.LogInformation("Successfully published outbox message {MessageId} of type {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                        msg.Id, msg.EventType, routing.Exchange, routing.RoutingKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId} of type {EventType}",
                        msg.Id, msg.EventType);

                    await this.MarkAsFailedAsync(msg, ex.Message, cancellationToken);
                }
            }
        }

        private async Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
        {
            return await _dbContext.OutboxMessages
                .Where(x => x.Status == OutboxMessageStatus.Pending)
                .OrderBy(x => x.OccurredOn)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        private async Task MarkAsProcessingAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            message.Status = OutboxMessageStatus.Processing;
            message.ProcessedOn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MarkAsSentAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            message.Status = OutboxMessageStatus.Success;
            message.ErrorMessage = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MarkAsFailedAsync(OutboxMessage message, string errorMessage, CancellationToken cancellationToken)
        {
            message.RetryCount++;
            message.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
            message.ProcessedOn = DateTime.UtcNow;

            if (message.RetryCount >= MaxRetryCount)
            {
                message.Status = OutboxMessageStatus.PermanentlyFailed;
            }
            else
            {
                message.Status = OutboxMessageStatus.Pending;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<IDomainEvent> DeserializeEventAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // For async signature consistency

            var assembly = typeof(IDomainEvent).Assembly;
            
            // Try to find event type in common namespaces
            var possibleNamespaces = new[]
            {
                "Flex.Domain.Events.Users",
                "Flex.Domain.Events"
            };

            Type? eventType = null;
            foreach (var ns in possibleNamespaces)
            {
                eventType = assembly.GetType($"{ns}.{message.EventType}");
                if (eventType != null)
                    break;
            }

            // If not found in namespaces, try to find by name only (searches all types in assembly)
            if (eventType == null)
            {
                eventType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == message.EventType && typeof(IDomainEvent).IsAssignableFrom(t));
            }

            if (eventType == null)
            {
                throw new InvalidOperationException(
                    $"Event type {message.EventType} not found in assembly {assembly.FullName}");
            }

            var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType, JsonOptions.Default);
            if (integrationEvent is not IDomainEvent evt)
            {
                throw new InvalidOperationException(
                    $"Deserialized object is not an IDomainEvent. Type: {integrationEvent?.GetType().Name ?? "null"}");
            }

            return evt;
        }
    }
}
