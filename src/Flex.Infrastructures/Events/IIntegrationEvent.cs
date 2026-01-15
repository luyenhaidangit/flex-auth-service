namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Marker interface for integration events.
    /// Integration events are used to communicate between microservices/bounded contexts.
    /// </summary>
    public interface IIntegrationEvent
    {
        /// <summary>
        /// Unique identifier for this event instance.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// When this event occurred (UTC).
        /// </summary>
        DateTime OccurredOn { get; }

        /// <summary>
        /// Event type name for routing/message type identification.
        /// </summary>
        string EventType { get; }
    }
}
