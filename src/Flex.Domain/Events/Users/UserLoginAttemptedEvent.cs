using Flex.Domain.Abstractions;

namespace Flex.Domain.Events.Users
{
    /// <summary>
    /// Represents a user login attempt (success or failure).
    /// This unified event replaces separate success/failed events.
    /// </summary>
    public sealed record UserLoginAttemptedEvent(
        long? UserId,
        string? UserName,
        string LoginType,
        string? IpAddress,
        bool IsSuccess
    ) : DomainEvent;
}
