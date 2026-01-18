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
        private readonly string _defaultExchange;
        public static class RoutingKeys
        {
            public const string UserLoginSuccess = "user.login.success";
            public const string UserLoginFailed = "user.login.failed";
        }


        public EventRoutingResolver(IOptions<RabbitMQOptions> options)
        {
            _defaultExchange = options.Value.ExchangeName;
        }

        public EventRouting Resolve(Type eventType)
        {
            var routingKey = this.GetRoutingKey(eventType);

            return new EventRouting
            {
                Exchange = _defaultExchange,
                RoutingKey = routingKey
            };
        }

        private string GetRoutingKey(Type eventType)
        {
            return eventType switch
            {
                _ when eventType == typeof(UserLoggedInSuccessEvent) => RoutingKeys.UserLoginSuccess,
                _ when eventType == typeof(UserLoggedInFailedEvent) => RoutingKeys.UserLoginFailed,
                _ => throw new InvalidOperationException(
                    $"No routing defined for event type: {eventType.Name}. " +
                    $"Please add routing configuration in {nameof(EventRoutingResolver)}.")
            };
        }
    }
}
