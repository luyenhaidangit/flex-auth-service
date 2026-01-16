using Flex.Domain.Abstractions;

namespace Flex.Domain.Events.Users
{
    /// <summary>
    /// Domain event raised when a user successfully logs in.
    /// This event is raised internally within the domain and should be mapped to an Integration Event
    /// if it needs to be published to other services.
    /// </summary>
    public sealed record UserLoggedInDomainEvent(
        long UserId,
        string UserName,
        string LoginType,  // ONLINE | TELLER
        string? IpAddress
    ) : DomainEvent;
}
