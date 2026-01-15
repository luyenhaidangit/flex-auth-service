namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Integration event published when a user login attempt is recorded.
    /// This event is sent to RabbitMQ for consumption by other services (e.g., audit service, analytics).
    /// </summary>
    public sealed record LoginHistoryIntegrationEvent(
        long UserId,
        string UserName,
        string LoginType,  // ONLINE | TELLER
        string? IpAddress,
        string? UserAgent,
        string Result,  // SUCCESS | FAILED
        string? FailureReason = null
    ) : IntegrationEvent;
}
