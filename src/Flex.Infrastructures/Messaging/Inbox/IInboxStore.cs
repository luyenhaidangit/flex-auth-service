namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Infrastructure service for inbox pattern deduplication.
    /// Uses UNIQUE constraint for atomic dedup - no race conditions.
    /// </summary>
    public interface IInboxStore
    {
        /// <summary>
        /// Atomically try to begin processing a message.
        /// Returns true if this is first time seeing this message.
        /// Returns false if duplicate (already processed/processing).
        /// Uses UNIQUE constraint on (MessageId, HandlerName) - no race condition.
        /// </summary>
        /// <param name="messageId">Unique message identifier from EventEnvelope</param>
        /// <param name="handlerName">Name of the handler processing this message</param>
        /// <param name="payload">Serialized message payload for audit</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if first time (proceed with processing), False if duplicate (skip)</returns>
        Task<bool> TryBeginProcessingAsync(
            Guid messageId,
            string handlerName,
            string payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark message as successfully processed.
        /// Should be called within same transaction as business logic.
        /// </summary>
        Task MarkProcessedAsync(
            Guid messageId,
            string handlerName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark message as failed with error details.
        /// Should be called within same transaction as business logic.
        /// </summary>
        Task MarkFailedAsync(
            Guid messageId,
            string handlerName,
            string errorMessage,
            CancellationToken cancellationToken = default);
    }
}
