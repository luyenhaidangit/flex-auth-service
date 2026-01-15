using Flex.Domain.Abstractions;

namespace Flex.Domain.Entities
{
    /// <summary>
    /// Entity representing an integration event stored in the outbox table.
    /// This implements the Transactional Outbox pattern to ensure reliable message delivery.
    /// </summary>
    public class OutboxMessage : EntityBase<long>
    {
        /// <summary>
        /// Event type name for routing.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Serialized event payload (JSON).
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// When the event occurred (UTC).
        /// </summary>
        public DateTime OccurredOn { get; set; }

        /// <summary>
        /// Processing status: Pending, Processing, Processed, Failed.
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Number of processing attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Error message if processing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// When the message was last processed (UTC).
        /// </summary>
        public DateTime? ProcessedOn { get; set; }
    }
}
