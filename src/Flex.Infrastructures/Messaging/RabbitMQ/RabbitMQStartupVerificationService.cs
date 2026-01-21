using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Hosted service that verifies RabbitMQ exchange existence during application startup.
    /// Application will fail to start if required exchange does not exist.
    /// </summary>
    internal sealed class RabbitMQStartupVerificationService : IHostedService
    {
        private readonly IRabbitMQExchangeVerifier _verifier;
        private readonly ILogger<RabbitMQStartupVerificationService> _logger;

        public RabbitMQStartupVerificationService(
            IRabbitMQExchangeVerifier verifier,
            ILogger<RabbitMQStartupVerificationService> logger)
        {
            _verifier = verifier;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting RabbitMQ exchange verification...");

            var isValid = await _verifier.VerifyExchangeAsync(cancellationToken);

            if (!isValid)
            {
                const string errorMessage =
                    "Application startup failed: Required RabbitMQ exchange does not exist. " +
                    "Please create the exchange before starting the application.";

                _logger.LogCritical(errorMessage);

                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("RabbitMQ exchange verification completed successfully");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // No cleanup needed
            return Task.CompletedTask;
        }
    }
}
