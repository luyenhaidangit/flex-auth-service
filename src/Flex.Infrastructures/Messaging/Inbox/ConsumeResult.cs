namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Result constants for message consumption indicating how RabbitMQ should handle the message.
    /// </summary>
    public static class ConsumeResult
    {
        /// <summary>
        /// Message processed successfully - ACK to RabbitMQ
        /// </summary>
        public const string Success = "SUCCESS";

        /// <summary>
        /// Transient error - NACK with requeue for retry
        /// </summary>
        public const string Retry = "RETRY";

        /// <summary>
        /// Permanent error - NACK without requeue (send to DLQ)
        /// </summary>
        public const string Dead = "DEAD";
    }
}
