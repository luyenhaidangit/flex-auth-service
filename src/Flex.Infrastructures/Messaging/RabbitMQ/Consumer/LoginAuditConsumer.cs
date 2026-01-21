using Flex.Domain.Events.Integration;
using Flex.Infrastructures.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Flex.Infrastructures.Messaging.RabbitMQ.Consumers
{
    public class LoginAuditConsumer : RabbitMQConsumerBase<LoginSuccessEvent>
    {
        private readonly ILogger<LoginAuditConsumer> _logger;

        public LoginAuditConsumer(
            IConnection connection,
            IOptions<RabbitMQOptions> options,
            IServiceProvider serviceProvider,
            ILogger<LoginAuditConsumer> logger) 
            : base(connection, options, serviceProvider, logger)
        {
            _logger = logger;
        }

        // Must match the queue name defined in definitions.json
        protected override string QueueName => "flex.audit.auth.q";
        
        // Process messages sequentially or with limited concurrency
        protected override ushort PrefetchCount => 20;

        protected override Task HandleMessageAsync(LoginSuccessEvent message, IServiceProvider scopeProvider, CancellationToken cancellationToken)
        {
            // TODO: Implement actual business logic (e.g. write to audit log table)
            // For now, we just log it to demonstrate the consumer is working
            
            _logger.LogInformation(
                "Audit Log: User {Username} ({UserId}) logged in from {IpAddress} at {Time}", 
                message.Username, 
                message.UserId, 
                message.IpAddress, 
                message.OccurredAt);

            return Task.CompletedTask;
        }
    }
}
