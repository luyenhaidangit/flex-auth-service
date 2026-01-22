using Flex.Domain.Events.Users;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flex.Auth.Consumers
{
    /// <summary>
    /// RabbitMQ consumer for UserLoggedInSuccessEvent.
    /// Thin wrapper - all logic delegated to base class and UserLoginSuccessHandler.
    /// This class only specifies the queue name - framework handles everything else.
    /// </summary>
    public class UserLoginConsumer : BaseRabbitMQConsumer<UserLoggedInSuccessEvent>
    {
        private const string QueueName = "q.auth.user-login-success";

        public UserLoginConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMQConsumer rabbitConsumer,
            ILogger<UserLoginConsumer> logger)
            : base(scopeFactory, rabbitConsumer, logger, QueueName)
        {
        }
    }
}
