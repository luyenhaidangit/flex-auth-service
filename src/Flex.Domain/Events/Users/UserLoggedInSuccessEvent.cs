using Flex.Domain.Abstractions;

namespace Flex.Domain.Events.Users
{
    public sealed record UserLoggedInSuccessEvent(
        long UserId,
        string UserName,
        string LoginType,
        string? IpAddress
    ) : DomainEvent;
}
