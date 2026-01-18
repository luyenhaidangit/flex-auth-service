namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    public sealed class RabbitMQOptions
    {
        public string HostName { get; init; } = default!;
        public int Port { get; init; } = 5672;
        public string UserName { get; init; } = default!;
        public string Password { get; init; } = default!;
        public string VirtualHost { get; init; } = "/";
        public int RequestedHeartbeat { get; init; } = 30;
        public int NetworkRecoveryIntervalSeconds { get; init; } = 10;
        public bool UseSsl { get; init; }
    }
}
