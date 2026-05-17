using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly RabbitMQOptions _options;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
            var cfg = _options;

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
                    cfg.NetworkRecoveryIntervalSeconds),
                ClientProvidedName = cfg.ClientProvidedName
            };

            _connection = factory.CreateConnection();

            _logger.LogInformation(
                "Connected to RabbitMQ server {HostName}:{Port}, virtual host '{VirtualHost}', exchange '{ExchangeName}'",
                cfg.HostName,
                cfg.Port,
                cfg.VirtualHost,
                cfg.ExchangeName);

            _connection.ConnectionShutdown += OnConnectionShutdown;

            if (_connection is IAutorecoveringConnection autorecoveringConnection)
            {
                autorecoveringConnection.RecoverySucceeded += OnRecoverySucceeded;
                autorecoveringConnection.ConnectionRecoveryError += OnConnectionRecoveryError;
            }
        }

        public Task PublishAsync(string exchange, string routingKey, byte[] body, IDictionary<string, object>? headers = null, CancellationToken ct = default)
        {
            using var channel = _connection.CreateModel();

            // Enable publisher confirms for reliable delivery
            channel.ConfirmSelect();

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = headers;

            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            // Wait for broker confirmation (throws on nack or timeout)
            var timeout = TimeSpan.FromSeconds(_options.PublisherConfirmTimeoutSeconds);
            channel.WaitForConfirmsOrDie(timeout);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _connection.ConnectionShutdown -= OnConnectionShutdown;

            if (_connection is IAutorecoveringConnection autorecoveringConnection)
            {
                autorecoveringConnection.RecoverySucceeded -= OnRecoverySucceeded;
                autorecoveringConnection.ConnectionRecoveryError -= OnConnectionRecoveryError;
            }

            _connection.Dispose();
        }

        private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
        {
            _logger.LogWarning(
                "RabbitMQ connection shut down. ReplyCode={ReplyCode}, ReplyText={ReplyText}",
                args.ReplyCode,
                args.ReplyText);
        }

        private void OnRecoverySucceeded(object? sender, EventArgs args)
        {
            _logger.LogInformation(
                "RabbitMQ connection recovered for server {HostName}:{Port}",
                _options.HostName,
                _options.Port);
        }

        private void OnConnectionRecoveryError(object? sender, ConnectionRecoveryErrorEventArgs args)
        {
            _logger.LogWarning(
                args.Exception,
                "RabbitMQ connection recovery failed for server {HostName}:{Port}",
                _options.HostName,
                _options.Port);
        }
    }
}
