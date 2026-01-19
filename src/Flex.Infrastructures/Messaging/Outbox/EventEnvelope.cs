namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Standard envelope for all events in the system.
    /// Provides metadata like ID, Source, Timestamp, and Versioning.
    /// </summary>
    public class EventEnvelope
    {
        /// <summary>
        /// Unique identifier for this specific message instance.
        /// Useful for de-duplication and tracing.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The service or component that originated the event.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The type of the event (e.g., "UserLoggedIn", "OrderCreated").
        /// Helps consumers identify how to deserialize the Data.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Version of the event schema.
        /// Allows for schema evolution (e.g., "1.0", "2.0").
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// When the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The actual domain event payload.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Optional metadata (e.g., TraceId, CorrelationId, TenantId).
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Creates a new EventEnvelope with the standard structure.
        /// </summary>
        public static EventEnvelope Create(object data, string source, string type, string version = "1.0")
        {
            return new EventEnvelope
            {
                Data = data,
                Source = source,
                Type = type,
                Version = version,
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
