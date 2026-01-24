using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ consumer implementation using RabbitMQ.Client.
    /// Supports graceful shutdown and configurable prefetch.
    /// </summary>
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IConnection _connection;
        private readonly RabbitMQConsumerOptions _options;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IModel? _channel;
        private string? _consumerTag;
        private bool _disposed;

        public RabbitMQConsumer(
            IOptions<RabbitMQConsumerOptions> options,
            ILogger<RabbitMQConsumer> logger)
        {
            _options = options.Value;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(_options.RequestedHeartbeat),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryIntervalSeconds),
                ClientProvidedName = $"{_options.ClientProvidedName}-consumer",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
        }

        public void Subscribe(string queueName, Func<byte[], CancellationToken, Task<bool>> handler, CancellationToken cancellationToken)
        {
            _channel = _connection.CreateModel();

            // Set QoS - prefetch count
            _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_options.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var success = await handler(ea.Body.ToArray(), cancellationToken);

                    if (success)
                    {
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        // Requeue for retry
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {Queue}", queueName);

                    // NACK without requeue to send to DLQ (if configured)
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming from queue {Queue} with tag {ConsumerTag}",
                queueName, _consumerTag);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null && _consumerTag != null)
            {
                try
                {
                    _channel.BasicCancel(_consumerTag);
                    _logger.LogInformation("Stopped consumer {ConsumerTag}", _consumerTag);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error stopping consumer {ConsumerTag}", _consumerTag);
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_channel != null)
                {
                    if (_channel.IsOpen)
                        _channel.Close();

                    _channel.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing channel");
            }

            try
            {
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                        _connection.Close();

                    _connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing connection");
            }
        }
    }
}
