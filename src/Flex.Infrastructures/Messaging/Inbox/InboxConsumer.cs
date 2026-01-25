using Flex.Infrastructures.Json;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Generic inbox consumer that wraps business handlers with deduplication.
    /// Ensures atomic transaction: Inbox + Business logic in same transaction.
    /// Application handlers remain clean - no infrastructure knowledge.
    /// </summary>
    public class InboxConsumer<TMessage>
    {
        private readonly IInboxStore _inboxStore;
        private readonly IMessageHandler<TMessage> _handler;
        private readonly IdentityDbContext _dbContext;
        private readonly ILogger<InboxConsumer<TMessage>> _logger;
        private readonly string _handlerName;

        public InboxConsumer(
            IInboxStore inboxStore,
            IMessageHandler<TMessage> handler,
            IdentityDbContext dbContext,
            ILogger<InboxConsumer<TMessage>> logger)
        {
            _inboxStore = inboxStore;
            _handler = handler;
            _dbContext = dbContext;
            _logger = logger;
            _handlerName = typeof(TMessage).Name;
        }

        /// <summary>
        /// Consume a message with inbox deduplication and atomic transaction.
        /// </summary>
        public async Task<ConsumeResult> ConsumeAsync(
            EventEnvelope envelope,
            CancellationToken cancellationToken)
        {
            // Start transaction - Inbox + Business will be atomic
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Try to begin processing (atomic dedup via UNIQUE constraint)
                var payload = JsonSerializer.Serialize(envelope, JsonOptions.Default);
                var isFirstTime = await _inboxStore.TryBeginProcessingAsync(
                    envelope.Id,
                    _handlerName,
                    payload,
                    cancellationToken);

                if (!isFirstTime)
                {
                    // Duplicate detected - rollback and ACK
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogInformation("Duplicate message {MessageId} for {Handler}, skipping",
                        envelope.Id, _handlerName);
                    return ConsumeResult.Ack;
                }

                // Deserialize and delegate to business handler
                var message = this.DeserializeMessage(envelope);
                await _handler.HandleAsync(message, cancellationToken);

                // Mark as processed
                await _inboxStore.MarkProcessedAsync(envelope.Id, _handlerName, cancellationToken);

                // Commit transaction - both inbox and business changes
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed message {MessageId} with {Handler}",
                    envelope.Id, _handlerName);

                return ConsumeResult.Ack;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                // Transient error - mark failed and retry
                await _inboxStore.MarkFailedAsync(envelope.Id, _handlerName, ex.Message, cancellationToken);
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogWarning(ex, "Retryable error processing {MessageId} with {Handler}",
                    envelope.Id, _handlerName);

                return ConsumeResult.Retry;
            }
            catch (Exception ex)
            {
                // Permanent error - mark failed and send to DLQ
                await _inboxStore.MarkFailedAsync(envelope.Id, _handlerName, ex.Message, cancellationToken);
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Permanent error processing {MessageId} with {Handler}",
                    envelope.Id, _handlerName);

                return ConsumeResult.DeadLetter;
            }
        }

        private TMessage DeserializeMessage(EventEnvelope envelope)
        {
            if (envelope.Data is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<TMessage>(
                    jsonElement.GetRawText(),
                    JsonOptions.Default)!;
            }

            return (TMessage)envelope.Data!;
        }

        private static bool IsRetryable(Exception ex)
        {
            // Define retryable exception types
            // Transient errors that should be retried
            return ex is TimeoutException
                || ex is HttpRequestException
                || ex is DbUpdateException
                || ex.InnerException is TimeoutException;
        }
    }
}
