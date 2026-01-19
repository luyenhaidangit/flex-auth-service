using Flex.Domain.Abstractions;
using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Json;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Implementation of IOutboxWriter that saves integration events to the database.
    /// </summary>
    public class OutboxWriter : IOutboxWriter
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IEventRoutingResolver _routingResolver;
        private readonly RabbitMQOptions _options;

        public OutboxWriter(
            IdentityDbContext dbContext,
            IEventRoutingResolver routingResolver,
            IOptions<RabbitMQOptions> options)
        {
            _dbContext = dbContext;
            _routingResolver = routingResolver;
            _options = options.Value;
        }

        public async Task AddAsync(IDomainEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            // Resolve routing when writing to outbox.
            var routing = _routingResolver.Resolve(integrationEvent.GetType());

            // Serialize event to JSON
            var envelope = EventEnvelope.Create(
                data: integrationEvent,
                source: _options.ClientProvidedName, 
                type: integrationEvent.GetType().Name, 
                version: "1.0");

            var payload = JsonSerializer.Serialize(envelope, envelope.GetType(), JsonOptions.Default);

            var outboxMessage = new OutboxMessage
            {
                EventType = integrationEvent.GetType().Name,
                Payload = payload, 
                Exchange = routing.Exchange,
                RoutingKey = routing.RoutingKey,
                OccurredOn = DateTime.UtcNow,
                Status = OutboxMessageStatus.Pending,
                RetryCount = 0
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }
    }
}
