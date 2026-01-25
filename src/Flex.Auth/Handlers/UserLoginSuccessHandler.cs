using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Domain.Events.Users;
using Flex.Infrastructures.Messaging.Inbox;
using Flex.Infrastructures.Persistence;

namespace Flex.Auth.Handlers
{
    /// <summary>
    /// Business logic for handling successful user login events.
    /// CLEAN - no infrastructure knowledge (no Inbox, no RabbitMQ, no EventEnvelope).
    /// Only knows about domain events and database.
    /// </summary>
    public class UserLoginSuccessHandler : IMessageHandler<UserLoggedInSuccessEvent>
    {
        private readonly IdentityDbContext _dbContext;
        private readonly ILogger<UserLoginSuccessHandler> _logger;

        public UserLoginSuccessHandler(
            IdentityDbContext dbContext,
            ILogger<UserLoginSuccessHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task HandleAsync(
            UserLoggedInSuccessEvent message,
            CancellationToken cancellationToken = default)
        {
            // Pure business logic - record login history
            var loginHistory = new LoginHistory
            {
                UserId = message.UserId,
                UserName = message.UserName,
                LoginType = message.LoginType,
                IpAddress = message.IpAddress,
                Result = LoginHistoryConstants.Result.Success,
                OccurredOn = DateTime.UtcNow
            };

            await _dbContext.LoginHistories.AddAsync(loginHistory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded login history for user {UserName} (Id: {UserId})",
                message.UserName, message.UserId);
        }
    }
}
