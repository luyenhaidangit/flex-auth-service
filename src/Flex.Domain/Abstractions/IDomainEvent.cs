namespace Flex.Domain.Abstractions
{
    /// <summary>
    /// Marker interface for domain events.
    /// Domain events represent something that happened in the domain that domain experts care about.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for this event instance.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// When this event occurred (UTC).
        /// </summary>
        DateTime OccurredOn { get; }
    }
}
