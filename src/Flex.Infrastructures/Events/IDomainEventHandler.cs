using Flex.Domain.Abstractions;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Generic interface for domain event handlers.
    /// </summary>
    public interface IDomainEventHandler<in TEvent>
        where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles the domain event.
        /// </summary>
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
