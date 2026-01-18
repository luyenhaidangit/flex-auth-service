using Flex.Domain.Abstractions;
using Flex.Domain.Constants;

namespace Flex.Domain.Entities
{
    /// <summary>
    /// Entity representing an integration event stored in the outbox table.
    /// This implements the Transactional Outbox pattern to ensure reliable message delivery.
    /// </summary>
    public class OutboxMessage : EntityBase<long>
    {
        /// <summary>
        /// Event type full name for deserialization if needed.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Serialized event payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// RabbitMQ exchange name. Resolved when writing to outbox.
        /// </summary>
        public string Exchange { get; set; } = string.Empty;

        /// <summary>
        /// RabbitMQ routing key. Resolved when writing to outbox.
        /// </summary>
        public string RoutingKey { get; set; } = string.Empty;

        /// <summary>
        /// When the event occurred (UTC).
        /// </summary>
        public DateTime OccurredOn { get; set; }

        /// <summary>
        /// Processing status: P (Pending), PR (Processing), C (Success), F (Failed), PF (PermanentlyFailed).
        /// </summary>
        public string Status { get; set; } = OutboxMessageStatus.Pending;

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
