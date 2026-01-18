using Flex.Domain.Events.Users;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Microsoft.Extensions.Options;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Default implementation of IEventRoutingResolver that maps event types to RabbitMQ routing.
    /// Application layer can override this to customize routing strategy.
    /// </summary>
    public sealed class DefaultEventRoutingResolver : IEventRoutingResolver
    {
        private readonly RabbitMQOptions _options;

        public DefaultEventRoutingResolver(IOptions<RabbitMQOptions> options)
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
                $"Please configure routing in {nameof(IEventRoutingResolver)} implementation or register a custom resolver.");
        }
    }
}
