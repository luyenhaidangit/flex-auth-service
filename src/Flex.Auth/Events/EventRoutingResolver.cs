using Flex.Domain.Events.Users;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Microsoft.Extensions.Options;

namespace Flex.Identity.Events
{
    /// <summary>
    /// Application-level implementation of IEventRoutingResolver that maps event types to RabbitMQ routing.
    /// This is where business logic for event routing is defined.
    /// </summary>
    public sealed class EventRoutingResolver : IEventRoutingResolver
    {
        private readonly RabbitMQOptions _options;

        public EventRoutingResolver(IOptions<RabbitMQOptions> options)
        {
            _options = options.Value;
        }

        public EventRouting Resolve(Type eventType)
        {
            var defaultExchange = _options.ExchangeName;

            if (eventType == typeof(UserLoggedInSuccessEvent))
            {
                return new EventRouting
                {
                    Exchange = defaultExchange,
                    RoutingKey = "user.login.success"
                };
            }

            if (eventType == typeof(UserLoggedInFailedEvent))
            {
                return new EventRouting
                {
                    Exchange = defaultExchange,
                    RoutingKey = "user.login.failed"
                };
            }

            throw new InvalidOperationException(
                $"No routing defined for {eventType.Name}. " +
                $"Please configure routing in {nameof(IEventRoutingResolver)} implementation.");
        }
    }
}
