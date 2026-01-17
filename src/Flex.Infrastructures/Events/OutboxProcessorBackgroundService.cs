using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Background service that periodically processes outbox messages and publishes them to RabbitMQ.
    /// </summary>
    public class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5); // Process every 5 seconds

        public OutboxProcessorBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<OutboxProcessorBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessorBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a new scope for each processing cycle to ensure proper lifetime management
                    using var scope = _serviceScopeFactory.CreateScope();
                    var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                    await outboxProcessor.ProcessPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox messages");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("OutboxProcessorBackgroundService stopped");
        }
    }
}
