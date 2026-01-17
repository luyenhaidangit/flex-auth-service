using Flex.Domain.Abstractions;
using Flex.Domain.Constants;
using Flex.Infrastructures.Json;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Implementation of IOutboxProcessor that reads pending messages from outbox and publishes them to RabbitMQ.
    /// </summary>
    public class OutboxProcessor : IOutboxProcessor
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ILogger<OutboxProcessor> _logger;

        public OutboxProcessor(
            IdentityDbContext dbContext,
            IRabbitMQPublisher rabbitMQPublisher,
            ILogger<OutboxProcessor> logger)
        {
            _dbContext = dbContext;
            _rabbitMQPublisher = rabbitMQPublisher;
            _logger = logger;
        }

        public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            // Get pending messages (limit to avoid processing too many at once)
            var pendingMessages = await _dbContext.OutboxMessages
                .Where(x => x.Status == OutboxMessageStatus.Pending)
                .OrderBy(x => x.OccurredOn)
                .Take(50) // Process in batches
                .ToListAsync(cancellationToken);

            if (!pendingMessages.Any())
            {
                return;
            }

            _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count);

            foreach (var message in pendingMessages)
            {
                try
                {
                    // Mark as processing
                    message.Status = OutboxMessageStatus.Processing;
                    message.ProcessedOn = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Deserialize and publish
                    var assembly = typeof(IDomainEvent).Assembly;
                    var eventType = assembly.GetType($"Flex.Infrastructures.Events.{message.EventType}");
                    if (eventType == null)
                    {
                        throw new InvalidOperationException($"Event type {message.EventType} not found in assembly {assembly.FullName}");
                    }

                    var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType, JsonOptions.Default);
                    if (integrationEvent is IDomainEvent evt)
                    {
                        await _rabbitMQPublisher.PublishAsync(evt, cancellationToken);

                        // Mark as processed
                        message.Status = OutboxMessageStatus.Processed;
                        message.ErrorMessage = null;
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Successfully published outbox message {MessageId} of type {EventType}",
                            message.Id, message.EventType);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Deserialized object is not an IIntegrationEvent");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId} of type {EventType}",
                        message.Id, message.EventType);

                    // Mark as failed and increment retry count
                    message.Status = OutboxMessageStatus.Failed;
                    message.RetryCount++;
                    message.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    message.ProcessedOn = DateTime.UtcNow;

                    // If retry count exceeds threshold, mark as permanently failed
                    if (message.RetryCount >= 5)
                    {
                        message.Status = OutboxMessageStatus.PermanentlyFailed;
                    }
                    else
                    {
                        // Reset to Pending for retry
                        message.Status = OutboxMessageStatus.Pending;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
