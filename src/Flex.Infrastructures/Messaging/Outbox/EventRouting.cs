namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Value Object representing RabbitMQ routing information (Exchange and RoutingKey).
    /// This is a domain concept that encapsulates routing configuration.
    /// </summary>
    public sealed record EventRouting
    {
        public string Exchange { get; init; } = default!;
        public string RoutingKey { get; init; } = default!;
    }
}
