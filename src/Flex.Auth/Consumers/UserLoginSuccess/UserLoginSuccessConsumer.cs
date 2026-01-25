using Flex.Domain.Events.Users;
using Flex.Infrastructures.Messaging.RabbitMQ;

namespace Flex.Identity.Consumers.UserLoginSuccess
{
    /// <summary>
    /// RabbitMQ consumer for UserLoggedInSuccessEvent.
    /// Thin wrapper - all logic delegated to base class and UserLoginSuccessHandler.
    /// This class only specifies the queue name - framework handles everything else.
    /// </summary>
    public class UserLoginConsumer : BaseRabbitMQConsumer<UserLoggedInSuccessEvent>
    {
        private const string QueueName = "q.audit.user-login-success";

        public UserLoginConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMQConsumer rabbitConsumer,
            ILogger<UserLoginConsumer> logger)
            : base(scopeFactory, rabbitConsumer, logger, QueueName)
        {
        }
    }
}
