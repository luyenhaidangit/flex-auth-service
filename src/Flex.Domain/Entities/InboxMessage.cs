using Flex.Domain.Abstractions;

namespace Flex.Domain.Entities
{
    /// <summary>
    /// Entity representing a processed message in the inbox table.
    /// This implements the Inbox pattern for message deduplication.
    /// </summary>
    public class InboxMessage : EntityBase<long>
    {
        /// <summary>
        /// Unique message identifier from EventEnvelope.Id
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Source service that published the event
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Type of event processed
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Name of the handler/consumer that processed this message
        /// </summary>
        public string HandlerName { get; set; } = string.Empty;

        /// <summary>
        /// Optional business key for correlation
        /// </summary>
        public string? BusinessKey { get; set; }

        /// <summary>
        /// Original message payload (JSON) for debugging/audit
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// When the message was first seen (UTC)
        /// </summary>
        public DateTime FirstSeenAt { get; set; }

        /// <summary>
        /// When the message was processed (UTC)
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Processing status: P (Processed), S (Skipped), F (Failed)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
