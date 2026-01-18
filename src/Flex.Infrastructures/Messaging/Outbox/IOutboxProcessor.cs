namespace Flex.Infrastructures.Messaging.Outbox
{
    public interface IOutboxProcessor
    {
        Task ProcessAsync(CancellationToken ct);
    }
}
