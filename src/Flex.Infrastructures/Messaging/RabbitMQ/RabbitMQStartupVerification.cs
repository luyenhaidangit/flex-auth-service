using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Hosted service that verifies RabbitMQ exchange existence during application startup.
    /// Application will fail to start if required exchange does not exist.
    /// </summary>
    internal sealed class RabbitMQStartupVerification : IHostedService
    {
        private readonly RabbitMQOptions _options;
        private readonly ILogger<RabbitMQStartupVerification> _logger;

        public RabbitMQStartupVerification(
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQStartupVerification> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Verifying RabbitMQ connection and exchange '{ExchangeName}'...", _options.ExchangeName);

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost,
                    RequestedHeartbeat = TimeSpan.FromSeconds(_options.RequestedHeartbeat),
                    ClientProvidedName = $"{_options.ClientProvidedName}-startup-verifier"
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                // Passive declaration: checks if exchange exists without creating it
                // Throws exception if exchange doesn't exist
                channel.ExchangeDeclarePassive(_options.ExchangeName);

                _logger.LogInformation(
                    "Successfully verified exchange '{ExchangeName}' exists on RabbitMQ server {HostName}",
                    _options.ExchangeName,
                    _options.HostName);

                return Task.CompletedTask;
            }
            catch (BrokerUnreachableException ex) when (ex.InnerException is AuthenticationFailureException)
            {
                var authEx = (AuthenticationFailureException)ex.InnerException;
                var errorMessage =
                    $"Application startup failed: RabbitMQ authentication failed. " +
                    $"Please check username and password configuration.";

                _logger.LogCritical(ex,
                    "RabbitMQ authentication failed for user '{UserName}' on server {HostName}:{Port}. " +
                    "Error: {ErrorMessage}",
                    _options.UserName,
                    _options.HostName,
                    _options.Port,
                    authEx.Message);

                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (BrokerUnreachableException ex)
            {
                var errorMessage =
                    $"Application startup failed: Cannot connect to RabbitMQ server. " +
                    $"Please check if RabbitMQ is running and network configuration is correct.";

                _logger.LogCritical(ex,
                    "Failed to connect to RabbitMQ server {HostName}:{Port}",
                    _options.HostName,
                    _options.Port);

                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
            {
                var errorMessage =
                    $"Application startup failed: Required RabbitMQ exchange '{_options.ExchangeName}' does not exist. " +
                    $"Please create the exchange before starting the application.";

                _logger.LogCritical(ex,
                    "Exchange '{ExchangeName}' not found on RabbitMQ server {HostName}",
                    _options.ExchangeName,
                    _options.HostName);

                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                var errorMessage =
                    $"Application startup failed: Unexpected error during RabbitMQ verification.";

                _logger.LogCritical(ex,
                    "Unexpected error verifying RabbitMQ exchange '{ExchangeName}' on server {HostName}:{Port}",
                    _options.ExchangeName,
                    _options.HostName,
                    _options.Port);

                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
