using Flex.Domain.Abstractions;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Interface for dispatching domain events to their handlers.
    /// Domain events are dispatched after SaveChanges to ensure transactional consistency.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Dispatches all domain events from entities that were saved in the current transaction.
        /// </summary>
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
