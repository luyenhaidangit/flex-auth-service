using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Implementation of IInboxStore using UNIQUE constraint for atomic deduplication.
    /// No race conditions - database guarantees atomicity.
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

        public async Task<bool> TryBeginProcessingAsync(
            Guid messageId,
            string handlerName,
            string payload,
            CancellationToken cancellationToken = default)
        {
            var inboxMessage = new InboxMessage
            {
                MessageId = messageId,
                Source = string.Empty, // Will be set later if needed
                EventType = string.Empty, // Will be set later if needed
                HandlerName = handlerName,
                Payload = payload,
                FirstSeenAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Status = InboxMessageStatus.Processed, // Optimistic - will update if fails
                RetryCount = 0
            };

            try
            {
                await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("First time seeing message {MessageId} for {Handler}",
                    messageId, handlerName);

                return true; // First time - proceed with processing
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Duplicate detected by UNIQUE constraint
                _logger.LogDebug("Duplicate message {MessageId} for {Handler} detected by UNIQUE constraint",
                    messageId, handlerName);

                return false; // Duplicate - skip processing
            }
        }

        public async Task MarkProcessedAsync(
            Guid messageId,
            string handlerName,
            CancellationToken cancellationToken = default)
        {
            var message = await _dbContext.InboxMessages
                .FirstOrDefaultAsync(x => x.MessageId == messageId && x.HandlerName == handlerName, cancellationToken);

            if (message != null)
            {
                message.Status = InboxMessageStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                message.ErrorMessage = null;

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Marked message {MessageId} as processed by {HandlerName}",
                    messageId, handlerName);
            }
        }

        public async Task MarkFailedAsync(
            Guid messageId,
            string handlerName,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            var message = await _dbContext.InboxMessages
                .FirstOrDefaultAsync(x => x.MessageId == messageId && x.HandlerName == handlerName, cancellationToken);

            if (message != null)
            {
                message.Status = InboxMessageStatus.Failed;
                message.ProcessedAt = DateTime.UtcNow;
                message.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
                message.RetryCount++;

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Marked message {MessageId} as failed (retry {RetryCount}): {Error}",
                    messageId, message.RetryCount, errorMessage);
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // PostgreSQL unique constraint violation: SqlState 23505 (UniqueViolation)
            return ex.InnerException is PostgresException postgresEx
                && postgresEx.SqlState == PostgresErrorCodes.UniqueViolation;
        }
    }
}
