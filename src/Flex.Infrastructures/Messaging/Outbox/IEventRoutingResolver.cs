namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Interface for resolving RabbitMQ routing information (Exchange and RoutingKey) from event type.
    /// Application layer decides routing, Infrastructure layer only publishes.
    /// </summary>
    public interface IEventRoutingResolver
    {
        /// <summary>
        /// Resolves exchange and routing key for the given event type.
        /// </summary>
        /// <param name="eventType">The type of the integration event.</param>
        /// <returns>Tuple containing (Exchange, RoutingKey).</returns>
        (string Exchange, string RoutingKey) Resolve(Type eventType);
    }
}
