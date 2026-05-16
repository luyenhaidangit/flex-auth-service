using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Implementation of IOutboxProcessor that reads pending messages from outbox and publishes them to RabbitMQ.
    /// </summary>
    internal sealed class OutboxProcessor : IOutboxProcessor
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IRabbitMQPublisher _publisher;
        private readonly ILogger<OutboxProcessor> _logger;

        private const int MaxRetryCount = 5;
        private const int DefaultBatchSize = 100;

        public OutboxProcessor(
            IdentityDbContext dbContext,
            IRabbitMQPublisher publisher,
            ILogger<OutboxProcessor> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            var messages = await this.GetPendingAsync(DefaultBatchSize, cancellationToken);

            if (!messages.Any())
            {
                return;
            }

            _logger.LogDebug("Processing {Count} pending outbox messages", messages.Count);

            foreach (var msg in messages)
            {
                try
                {
                    // Publish to RabbitMQ using routing info stored in outbox
                    var body = Encoding.UTF8.GetBytes(msg.Payload);
                    await _publisher.PublishAsync(msg.Exchange, msg.RoutingKey, body, null, cancellationToken);

                    await this.MarkAsSentAsync(msg, cancellationToken);

                    _logger.LogInformation("Successfully published outbox message {MessageId} of type {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                        msg.Id, msg.EventType, msg.Exchange, msg.RoutingKey);
                }
                catch (Exception ex)
                {
                    await this.MarkAsFailedAsync(msg, ex.Message, cancellationToken);

                    _logger.LogError(ex, "Failed to publish outbox message {MessageId} of type {EventType}. Retry={RetryCount}, Status={Status}",
                        msg.Id, msg.EventType, msg.RetryCount, msg.Status);

                    if (msg.Status == OutboxMessageStatus.Dead)
                    {
                        _logger.LogWarning("Outbox message {MessageId} of type {EventType} reached max retry count {RetryCount} and was marked as dead",
                            msg.Id, msg.EventType, msg.RetryCount);
                    }
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

        private async Task MarkAsSentAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            message.Status = OutboxMessageStatus.Sent;
            message.ErrorMessage = null;
            message.ProcessedOn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MarkAsFailedAsync(OutboxMessage message, string errorMessage, CancellationToken cancellationToken)
        {
            message.RetryCount++;
            message.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
            message.ProcessedOn = DateTime.UtcNow;

            if (message.RetryCount >= MaxRetryCount)
            {
                message.Status = OutboxMessageStatus.Dead;
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
