namespace Flex.Domain.Abstractions
{
    /// <summary>
    /// Base class for domain events.
    /// Use record types for immutability and value equality.
    /// </summary>
    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
