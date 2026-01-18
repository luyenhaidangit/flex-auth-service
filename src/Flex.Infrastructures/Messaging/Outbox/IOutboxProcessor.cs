namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Interface for processing outbox messages and publishing them to RabbitMQ.
    /// </summary>
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Processes pending outbox messages and publishes them to RabbitMQ.
        /// </summary>
        Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
    }
}
