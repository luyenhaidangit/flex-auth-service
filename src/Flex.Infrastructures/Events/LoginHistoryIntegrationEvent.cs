namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Integration event published when a user login attempt is recorded.
    /// </summary>
    public sealed record LoginHistoryIntegrationEvent(
        long UserId,
        string UserName,
        string LoginType,
        string? IpAddress,
        string Result,
        string? FailureReason = null
    ) : IntegrationEvent;
}
