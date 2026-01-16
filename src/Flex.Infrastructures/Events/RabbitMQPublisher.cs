using Flex.Domain.Abstractions;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Implementation of IRabbitMQPublisher that publishes events to RabbitMQ.
    /// </summary>
    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly RabbitMQOptions _options;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RabbitMQPublisher(RabbitMQOptions options)
        {
            _options = options;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.ConnectionString)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: options.ExchangeName,
                type: options.ExchangeType,
                durable: options.ExchangeDurable,
                autoDelete: options.ExchangeAutoDelete);
        }

        public Task PublishAsync(IDomainEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.MessageId = integrationEvent.EventId.ToString();
                properties.Timestamp = new AmqpTimestamp(
                    new DateTimeOffset(integrationEvent.OccurredOn).ToUnixTimeSeconds());
                properties.Type = integrationEvent.EventType;
                properties.Persistent = true; // Make messages persistent

                // Routing key format: event.type (e.g., "login.history")
                var routingKey = integrationEvent.EventType
                    .Replace("IntegrationEvent", "")
                    .ToLowerInvariant()
                    .Replace(".", "-");

                _channel.BasicPublish(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log error and rethrow - caller should handle retry
                throw new InvalidOperationException(
                    $"Failed to publish integration event {integrationEvent.EventType} to RabbitMQ", ex);
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
