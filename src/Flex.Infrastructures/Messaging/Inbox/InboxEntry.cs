namespace Flex.Infrastructures.Messaging.Inbox
{
    public record InboxEntry
    {
        public Guid MessageId { get; init; }
        public string Source { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public string HandlerName { get; init; } = string.Empty;
        public string? BusinessKey { get; init; }
        public string Payload { get; init; } = string.Empty;
    }
}
