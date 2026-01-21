namespace Flex.Domain.Entities
{
    /// <summary>
    /// Entity representing a processed message in the inbox table.
    /// This implements the Idempotent Consumer pattern.
    /// </summary>
    public class InboxMessage
    {
        /// <summary>
        /// Unique key to identify the message (usually MessageId or a business key).
        /// </summary>
        public Guid IdempotencyKey { get; set; }

        /// <summary>
        /// Name of the consumer group that processed the message.
        /// </summary>
        public string Consumer { get; set; } = string.Empty;

        /// <summary>
        /// When the message was processed (UTC).
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// (Optional) Original message payload for auditing/debugging.
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// (Optional) When the message originally occurred (UTC).
        /// </summary>
        public DateTime? OccurredAt { get; set; }
    }
}
