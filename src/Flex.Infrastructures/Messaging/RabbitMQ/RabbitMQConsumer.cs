using Flex.Infrastructures.Messaging.Inbox;
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
        private readonly ConnectionFactory _factory;
        private readonly RabbitMQConsumerOptions _options;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private string? _consumerTag;
        private string? _queueName;
        private CancellationTokenSource? _subscriptionCts;
        private Task? _subscriptionTask;
        private bool _disposed;
        private readonly CountdownEvent _inflight = new CountdownEvent(0);

        public RabbitMQConsumer(
            IOptions<RabbitMQConsumerOptions> options,
            ILogger<RabbitMQConsumer> logger)
        {
            _options = options.Value;
            _logger = logger;

            _factory = new ConnectionFactory
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
        }

        public void Subscribe(string queueName, Func<byte[], CancellationToken, Task<ConsumeResult>> handler, CancellationToken cancellationToken)
        {
            _queueName = queueName;
            _subscriptionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _subscriptionTask = Task.Run(
                () => SubscribeWithRetryAsync(queueName, handler, _subscriptionCts.Token),
                CancellationToken.None);
        }

        private async Task SubscribeWithRetryAsync(
            string queueName,
            Func<byte[], CancellationToken, Task<ConsumeResult>> handler,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Connect();
                    StartConsuming(queueName, handler, cancellationToken);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    CleanupConnection();

                    _logger.LogWarning(
                        ex,
                        "Cannot connect RabbitMQ consumer to server {HostName}:{Port} for queue {Queue}. Retrying in {DelaySeconds} seconds.",
                        _options.HostName,
                        _options.Port,
                        queueName,
                        _options.NetworkRecoveryIntervalSeconds);

                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.NetworkRecoveryIntervalSeconds),
                        cancellationToken);
                }
            }
        }

        private void Connect()
        {
            _connection = _factory.CreateConnection();

            _logger.LogInformation(
                "Connected RabbitMQ consumer to server {HostName}:{Port}, virtual host '{VirtualHost}', exchange '{ExchangeName}'",
                _options.HostName,
                _options.Port,
                _options.VirtualHost,
                _options.ExchangeName);

            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.ConnectionBlocked += OnConnectionBlocked;
            _connection.ConnectionUnblocked += OnConnectionUnblocked;

            if (_connection is IAutorecoveringConnection autorecoveringConnection)
            {
                autorecoveringConnection.RecoverySucceeded += OnRecoverySucceeded;
                autorecoveringConnection.ConnectionRecoveryError += OnConnectionRecoveryError;
                autorecoveringConnection.RecoveringConsumer += OnRecoveringConsumer;
            }
        }

        private void StartConsuming(
            string queueName,
            Func<byte[], CancellationToken, Task<ConsumeResult>> handler,
            CancellationToken cancellationToken)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("RabbitMQ connection has not been created.");
            }

            _channel = _connection.CreateModel();
            _channel.ModelShutdown += OnModelShutdown;
            _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_options.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            _consumerTag = _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation(
                "Started consuming from RabbitMQ queue {Queue} with tag {ConsumerTag}",
                queueName,
                _consumerTag);

            consumer.Received += async (model, ea) =>
            {
                _inflight.AddCount();

                try
                {
                    var result = await handler(ea.Body.ToArray(), cancellationToken);

                    switch (result)
                    {
                        case ConsumeResult.Ack:
                            _channel.BasicAck(ea.DeliveryTag, multiple: false);
                            break;

                        case ConsumeResult.Retry:
                            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                            break;

                        case ConsumeResult.DeadLetter:
                            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                            break;

                        default:
                            _logger.LogWarning("Unknown ConsumeResult {Result}, treating as DeadLetter", result);
                            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {Queue}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // NAK
                }
                finally
                {
                    _inflight.Signal();
                }
            };
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _subscriptionCts?.Cancel();

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

            try
            {
                _inflight.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("StopAsync cancelled while waiting inflight messages");
            }

            if (_subscriptionTask != null)
            {
                try
                {
                    await _subscriptionTask.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("StopAsync cancelled while waiting RabbitMQ consumer subscription task");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _subscriptionCts?.Cancel();

            try
            {
                if (_channel != null)
                {
                    _channel.ModelShutdown -= OnModelShutdown;

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
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.ConnectionUnblocked -= OnConnectionUnblocked;

                    if (_connection is IAutorecoveringConnection autorecoveringConnection)
                    {
                        autorecoveringConnection.RecoverySucceeded -= OnRecoverySucceeded;
                        autorecoveringConnection.ConnectionRecoveryError -= OnConnectionRecoveryError;
                        autorecoveringConnection.RecoveringConsumer -= OnRecoveringConsumer;
                    }

                    if (_connection.IsOpen)
                        _connection.Close();

                    _connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing connection");
            }

            _subscriptionCts?.Dispose();
        }

        private void CleanupConnection()
        {
            try
            {
                if (_channel != null)
                {
                    _channel.ModelShutdown -= OnModelShutdown;

                    if (_channel.IsOpen)
                        _channel.Close();

                    _channel.Dispose();
                    _channel = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up RabbitMQ consumer channel");
            }

            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.ConnectionUnblocked -= OnConnectionUnblocked;

                    if (_connection is IAutorecoveringConnection autorecoveringConnection)
                    {
                        autorecoveringConnection.RecoverySucceeded -= OnRecoverySucceeded;
                        autorecoveringConnection.ConnectionRecoveryError -= OnConnectionRecoveryError;
                        autorecoveringConnection.RecoveringConsumer -= OnRecoveringConsumer;
                    }

                    if (_connection.IsOpen)
                        _connection.Close();

                    _connection.Dispose();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up RabbitMQ consumer connection");
            }
        }

        private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
        {
            _logger.LogWarning(
                "RabbitMQ consumer connection shut down for queue {Queue}. ReplyCode={ReplyCode}, ReplyText={ReplyText}",
                _queueName,
                args.ReplyCode,
                args.ReplyText);
        }

        private void OnModelShutdown(object? sender, ShutdownEventArgs args)
        {
            _logger.LogWarning(
                "RabbitMQ consumer channel shut down for queue {Queue}. ReplyCode={ReplyCode}, ReplyText={ReplyText}",
                _queueName,
                args.ReplyCode,
                args.ReplyText);
        }

        private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs args)
        {
            _logger.LogWarning(
                "RabbitMQ consumer connection blocked for queue {Queue}. Reason={Reason}",
                _queueName,
                args.Reason);
        }

        private void OnConnectionUnblocked(object? sender, EventArgs args)
        {
            _logger.LogInformation(
                "RabbitMQ consumer connection unblocked for queue {Queue}",
                _queueName);
        }

        private void OnRecoverySucceeded(object? sender, EventArgs args)
        {
            _logger.LogInformation(
                "RabbitMQ consumer connection recovered for queue {Queue} on server {HostName}:{Port}",
                _queueName,
                _options.HostName,
                _options.Port);
        }

        private void OnConnectionRecoveryError(object? sender, ConnectionRecoveryErrorEventArgs args)
        {
            _logger.LogWarning(
                args.Exception,
                "RabbitMQ consumer connection recovery failed for queue {Queue} on server {HostName}:{Port}",
                _queueName,
                _options.HostName,
                _options.Port);
        }

        private void OnRecoveringConsumer(object? sender, RecoveringConsumerEventArgs args)
        {
            _logger.LogInformation(
                "RabbitMQ consumer is recovering subscription for queue {Queue}",
                _queueName);
        }
    }
}
