using Flex.Domain.Abstractions;
using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Json;
using Flex.Infrastructures.Persistence;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Implementation of IOutboxWriter that saves integration events to the database.
    /// </summary>
    public class OutboxWriter : IOutboxWriter
    {
        private readonly IdentityDbContext _dbContext;

        public OutboxWriter(IdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(IDomainEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions.Default);

            var outboxMessage = new OutboxMessage
            {
                EventType = integrationEvent.EventType,
                Payload = payload,
                OccurredOn = integrationEvent.OccurredOn,
                Status = OutboxMessageStatus.Pending,
                RetryCount = 0
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }
    }
}
