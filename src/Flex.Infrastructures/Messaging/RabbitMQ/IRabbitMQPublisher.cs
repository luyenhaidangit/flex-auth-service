namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync(string exchange, string routingKey,byte[] body,IDictionary<string, object>? headers = null,CancellationToken ct = default);
    }
}
