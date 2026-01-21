using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

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
            _logger.LogInformation("Verifying RabbitMQ exchange '{ExchangeName}' exists...", _options.ExchangeName);

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
            catch (Exception ex)
            {
                const string errorMessage =
                    "Application startup failed: Required RabbitMQ exchange does not exist. " +
                    "Please create the exchange before starting the application.";

                _logger.LogCritical(ex, errorMessage + " Exchange: '{ExchangeName}', Server: {HostName}",
                    _options.ExchangeName,
                    _options.HostName);

                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // No cleanup needed
            return Task.CompletedTask;
        }
    }
}
