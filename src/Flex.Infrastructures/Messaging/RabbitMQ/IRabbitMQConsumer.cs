using Flex.Infrastructures.Messaging.Inbox;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Interface for RabbitMQ consumer operations.
    /// </summary>
    public interface IRabbitMQConsumer : IDisposable
    {
        /// <summary>
        /// Subscribe to a queue and process messages.
        /// </summary>
        /// <param name="queueName">Name of the queue to consume from</param>
        /// <param name="handler">Message handler that returns ConsumeResult to indicate ACK/NACK behavior</param>
        /// <param name="cancellationToken">Cancellation token</param>
        void Subscribe(string queueName, Func<byte[], CancellationToken, Task<ConsumeResult>> handler, CancellationToken cancellationToken);

        /// <summary>
        /// Stop consuming messages gracefully.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
