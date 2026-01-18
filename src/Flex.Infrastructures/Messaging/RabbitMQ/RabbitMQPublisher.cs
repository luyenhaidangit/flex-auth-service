using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly IConnection _connection;

        public RabbitMQPublisher(IOptions<RabbitMQOptions> options)
        {
            var cfg = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = cfg.HostName,
                Port = cfg.Port,
                UserName = cfg.UserName,
                Password = cfg.Password,
                VirtualHost = cfg.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(cfg.RequestedHeartbeat),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(
                    cfg.NetworkRecoveryIntervalSeconds)
                ClientProvidedName = cfg.ClientProvidedName
            };

            _connection = factory.CreateConnection();
        }

        public Task PublishAsync(string exchange,string routingKey,byte[] body,IDictionary<string, object>? headers = null,CancellationToken ct = default)
        {
            using var channel = _connection.CreateModel();

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = headers;

            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
