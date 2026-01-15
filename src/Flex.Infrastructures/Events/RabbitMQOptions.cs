namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Configuration options for RabbitMQ connection and publishing.
    /// </summary>
    public class RabbitMQOptions
    {
        /// <summary>
        /// RabbitMQ connection string (e.g., "amqp://guest:guest@localhost:5672/").
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Exchange name for publishing events.
        /// </summary>
        public string ExchangeName { get; set; } = "flex.events";

        /// <summary>
        /// Exchange type: "direct", "topic", "fanout", "headers".
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// Whether the exchange is durable.
        /// </summary>
        public bool ExchangeDurable { get; set; } = true;

        /// <summary>
        /// Whether the exchange is auto-deleted when not in use.
        /// </summary>
        public bool ExchangeAutoDelete { get; set; } = false;

        /// <summary>
        /// Maximum number of retry attempts for publishing.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds.
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 1000;
    }
}
