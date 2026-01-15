using Flex.Domain.Events.Users;
using Flex.Infrastructures.Events;

namespace Flex.Identity.Handlers
{
    /// <summary>
    /// Handler for UserLoggedInDomainEvent.
    /// Maps the domain event to an integration event and writes it to the outbox.
    /// </summary>
    public class UserLoggedInHandler : IDomainEventHandler<UserLoggedInDomainEvent>
    {
        private readonly IOutboxWriter _outboxWriter;

        public UserLoggedInHandler(IOutboxWriter outboxWriter)
        {
            _outboxWriter = outboxWriter;
        }

        public async Task Handle(UserLoggedInDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            // Map domain event to integration event
            var integrationEvent = new LoginHistoryIntegrationEvent(
                UserId: domainEvent.UserId,
                UserName: domainEvent.UserName,
                LoginType: domainEvent.LoginType,
                IpAddress: domainEvent.IpAddress,
                UserAgent: domainEvent.UserAgent,
                Result: "SUCCESS"
            );

            // Write to outbox for later publishing
            await _outboxWriter.AddAsync(integrationEvent, cancellationToken);
        }
    }
}
