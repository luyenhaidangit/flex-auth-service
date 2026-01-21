namespace Flex.Domain.Events.Integration
{
    public class LoginSuccessEvent
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
