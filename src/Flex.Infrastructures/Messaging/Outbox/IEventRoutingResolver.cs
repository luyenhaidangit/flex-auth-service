namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Interface for resolving RabbitMQ routing information from event type.
    /// Application layer decides routing, Infrastructure layer only publishes.
    /// </summary>
    public interface IEventRoutingResolver
    {
        /// <summary>
        /// Resolves routing information for the given event type.
        /// </summary>
        /// <param name="eventType">The type of the integration event.</param>
        /// <returns>EventRouting value object containing Exchange and RoutingKey.</returns>
        EventRouting Resolve(Type eventType);
    }
}
