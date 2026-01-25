using Flex.Infrastructures.Json;
using Flex.Infrastructures.Messaging.Inbox;
using Flex.Infrastructures.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Base class for RabbitMQ consumers with inbox pattern.
    /// Handles RabbitMQ concerns (ACK/NACK/QoS) while delegating business logic to InboxConsumer.
    /// Subclasses only need to specify queue name - all logic is handled by framework.
    /// </summary>
    public abstract class BaseRabbitMQConsumer<TMessage> : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMQConsumer _rabbitConsumer;
        private readonly ILogger _logger;
        private readonly string _queueName;

        protected BaseRabbitMQConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMQConsumer rabbitConsumer,
            ILogger logger,
            string queueName)
        {
            _scopeFactory = scopeFactory;
            _rabbitConsumer = rabbitConsumer;
            _logger = logger;
            _queueName = queueName;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rabbitConsumer.Subscribe(
                queueName: _queueName,
                handler: this.HandleMessageAsync,
                cancellationToken: stoppingToken);

            _logger.LogInformation("{Consumer} started, listening on queue {Queue}",
                GetType().Name, _queueName);

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Consumer} stopping...", GetType().Name);
            await _rabbitConsumer.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("{Consumer} stopped", GetType().Name);
        }

        private async Task<ConsumeResult> HandleMessageAsync(byte[] body, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var inboxConsumer = scope.ServiceProvider.GetRequiredService<InboxConsumer<TMessage>>();

            // Deserialize envelope
            var json = Encoding.UTF8.GetString(body);
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, JsonOptions.Default);

            if (envelope == null)
            {
                _logger.LogWarning("Failed to deserialize envelope");
                return ConsumeResult.Ack; // Prevent infinite redelivery
            }

            // Delegate to inbox consumer - returns ConsumeResult directly
            var result = await inboxConsumer.ConsumeAsync(envelope, cancellationToken);

            return result;
        }
    }
}
