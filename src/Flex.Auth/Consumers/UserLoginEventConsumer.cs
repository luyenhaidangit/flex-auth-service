using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Domain.Events.Users;
using Flex.Infrastructures.Json;
using Flex.Infrastructures.Messaging.Inbox;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Flex.Infrastructures.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Flex.Auth.Consumers
{
    /// <summary>
    /// Consumer for UserLoggedInSuccessEvent with inbox-based deduplication.
    /// Writes login history to database when user login events are received.
    /// </summary>
    public sealed class UserLoginEventConsumer : BackgroundService
    {
        private const string QueueName = "q.auth.user-login-success";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMQConsumer _consumer;
        private readonly ILogger<UserLoginEventConsumer> _logger;

        public UserLoginEventConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMQConsumer consumer,
            ILogger<UserLoginEventConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _consumer = consumer;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(
                queueName: QueueName,
                handler: HandleMessageAsync,
                cancellationToken: stoppingToken);

            _logger.LogInformation("UserLoginEventConsumer started, listening on queue {Queue}", QueueName);

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UserLoginEventConsumer stopping...");
            await _consumer.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("UserLoginEventConsumer stopped");
        }

        private async Task<bool> HandleMessageAsync(byte[] body, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            // Deserialize envelope
            var json = Encoding.UTF8.GetString(body);
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, JsonOptions.Default);

            if (envelope == null)
            {
                _logger.LogWarning("Failed to deserialize message envelope");
                return true; // ACK to avoid infinite redelivery
            }

            // Deduplication check
            if (await inbox.ExistsAsync(envelope.Id, cancellationToken))
            {
                _logger.LogInformation("Duplicate message {MessageId}, skipping", envelope.Id);
                return true; // ACK
            }

            var inboxEntry = new InboxEntry
            {
                MessageId = envelope.Id,
                Source = envelope.Source,
                EventType = envelope.Type,
                HandlerName = nameof(UserLoginEventConsumer),
                Payload = json
            };

            try
            {
                // Extract event data from envelope
                var eventData = DeserializeEventData(envelope);

                if (eventData != null)
                {
                    // Write to LOGIN_HISTORIES table
                    var loginHistory = new LoginHistory
                    {
                        UserId = eventData.UserId,
                        UserName = eventData.UserName,
                        LoginType = eventData.LoginType,
                        IpAddress = eventData.IpAddress,
                        Result = LoginHistoryConstants.Result.Success,
                        OccurredOn = envelope.OccurredOn.UtcDateTime
                    };

                    await dbContext.LoginHistories.AddAsync(loginHistory, cancellationToken);

                    // Update inbox entry with business key
                    inboxEntry = inboxEntry with { BusinessKey = eventData.UserId.ToString() };
                }

                // Mark as processed AFTER successful processing
                await inbox.MarkProcessedAsync(inboxEntry, cancellationToken);

                _logger.LogInformation(
                    "Successfully processed login event for user {UserName} (Id: {UserId})",
                    eventData?.UserName, eventData?.UserId);

                return true; // ACK
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId}", envelope.Id);

                // Mark as failed for audit
                await inbox.MarkFailedAsync(inboxEntry, ex.Message, cancellationToken);

                return false; // NACK for retry
            }
        }

        private static UserLoggedInSuccessEvent? DeserializeEventData(EventEnvelope envelope)
        {
            if (envelope.Data == null)
                return null;

            // Data might be JsonElement if deserialized from JSON
            if (envelope.Data is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<UserLoggedInSuccessEvent>(
                    jsonElement.GetRawText(),
                    JsonOptions.Default);
            }

            // Direct cast if already correct type
            return envelope.Data as UserLoggedInSuccessEvent;
        }
    }
}
