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

        public (string Exchange, string RoutingKey) Resolve(Type eventType)
        {
            var defaultExchange = _options.ExchangeName;

            return eventType.Name switch
            {
                nameof(UserLoggedInSuccessEvent) => (defaultExchange, "user.login.success"),
                nameof(UserLoggedInFailedEvent) => (defaultExchange, "user.login.failed"),
                _ => throw new InvalidOperationException(
                    $"No routing configuration found for event type: {eventType.Name}. " +
                    $"Please configure routing in {nameof(IEventRoutingResolver)} implementation or register a custom resolver.")
            };
        }
    }
}
