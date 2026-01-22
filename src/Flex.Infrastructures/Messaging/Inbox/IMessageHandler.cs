namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Application-level message handler interface.
    /// Implementations should contain ONLY business logic - no infrastructure concerns.
    /// Handler does not know about Inbox, RabbitMQ, or EventEnvelope.
    /// </summary>
    /// <typeparam name="TMessage">The domain event type to handle</typeparam>
    public interface IMessageHandler<in TMessage>
    {
        /// <summary>
        /// Handle the message with pure business logic.
        /// Throw exceptions for failures - infrastructure will handle retry/DLQ.
        /// </summary>
        /// <param name="message">The domain event message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
