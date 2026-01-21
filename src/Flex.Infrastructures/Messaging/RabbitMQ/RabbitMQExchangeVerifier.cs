using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Implementation of exchange verification service.
    /// Uses passive declaration to check exchange existence without modifying server state.
    /// </summary>
    internal sealed class RabbitMQExchangeVerifier : IRabbitMQExchangeVerifier
    {
        private readonly RabbitMQOptions _options;
        private readonly ILogger<RabbitMQExchangeVerifier> _logger;

        public RabbitMQExchangeVerifier(
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQExchangeVerifier> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task<bool> VerifyExchangeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var factory = CreateConnectionFactory();

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                // Passive declaration: checks if exchange exists without creating it
                // Throws exception if exchange doesn't exist
                channel.ExchangeDeclarePassive(_options.ExchangeName);

                _logger.LogInformation(
                    "Successfully verified exchange '{ExchangeName}' exists on RabbitMQ server {HostName}",
                    _options.ExchangeName,
                    _options.HostName);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to verify exchange '{ExchangeName}' on RabbitMQ server {HostName}. " +
                    "Please ensure the exchange exists before starting the application.",
                    _options.ExchangeName,
                    _options.HostName);

                return Task.FromResult(false);
            }
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(_options.RequestedHeartbeat),
                ClientProvidedName = $"{_options.ClientProvidedName}-verifier"
            };
        }
    }
}
