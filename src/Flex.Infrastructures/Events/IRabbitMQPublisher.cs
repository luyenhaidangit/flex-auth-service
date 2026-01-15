namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Interface for publishing integration events to RabbitMQ.
    /// </summary>
    public interface IRabbitMQPublisher
    {
        /// <summary>
        /// Publishes an integration event to RabbitMQ.
        /// </summary>
        Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
    }
}
