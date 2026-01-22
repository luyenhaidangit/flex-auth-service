namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Interface for inbox message deduplication store.
    /// </summary>
    public interface IInboxStore
    {
        /// <summary>
        /// Checks if a message has already been processed.
        /// </summary>
        Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as processed in the inbox.
        /// </summary>
        Task MarkProcessedAsync(InboxEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as failed in the inbox.
        /// </summary>
        Task MarkFailedAsync(InboxEntry entry, string errorMessage, CancellationToken cancellationToken = default);
    }
}
