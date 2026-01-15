namespace Flex.Domain.Abstractions
{
    public abstract class EntityBase<TId>
    {
        public TId Id { get; protected set; } = default!;

        private readonly List<IDomainEvent> _domainEvents = new();
        
        /// <summary>
        /// Read-only collection of domain events raised by this entity.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Raises a domain event. The event will be dispatched after SaveChanges.
        /// </summary>
        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all domain events. Called after events have been dispatched.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
