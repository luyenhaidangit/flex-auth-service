using Flex.Domain.Abstractions;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Interface for writing integration events to the outbox table.
    /// This ensures events are saved in the same transaction as domain changes.
    /// </summary>
    public interface IOutboxWriter
    {
        /// <summary>
        /// Adds an integration event to the outbox for later publishing.
        /// </summary>
        Task AddAsync(IDomainEvent integrationEvent, CancellationToken cancellationToken = default);
    }
}
