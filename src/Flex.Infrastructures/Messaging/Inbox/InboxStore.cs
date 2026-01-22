using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Implementation of IInboxStore that manages message deduplication using database.
    /// </summary>
    internal sealed class InboxStore : IInboxStore
    {
        private readonly IdentityDbContext _dbContext;
        private readonly ILogger<InboxStore> _logger;

        public InboxStore(IdentityDbContext dbContext, ILogger<InboxStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.InboxMessages
                .AnyAsync(x => x.MessageId == messageId, cancellationToken);
        }

        public async Task MarkProcessedAsync(InboxEntry entry, CancellationToken cancellationToken = default)
        {
            var inboxMessage = new InboxMessage
            {
                MessageId = entry.MessageId,
                Source = entry.Source,
                EventType = entry.EventType,
                HandlerName = entry.HandlerName,
                BusinessKey = entry.BusinessKey,
                Payload = entry.Payload,
                FirstSeenAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Status = InboxMessageStatus.Processed
            };

            await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked message {MessageId} as processed by {HandlerName}",
                entry.MessageId, entry.HandlerName);
        }

        public async Task MarkFailedAsync(InboxEntry entry, string errorMessage, CancellationToken cancellationToken = default)
        {
            var inboxMessage = new InboxMessage
            {
                MessageId = entry.MessageId,
                Source = entry.Source,
                EventType = entry.EventType,
                HandlerName = entry.HandlerName,
                BusinessKey = entry.BusinessKey,
                Payload = entry.Payload,
                FirstSeenAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Status = InboxMessageStatus.Failed,
                ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage
            };

            await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Marked message {MessageId} as failed: {Error}",
                entry.MessageId, errorMessage);
        }
    }
}
