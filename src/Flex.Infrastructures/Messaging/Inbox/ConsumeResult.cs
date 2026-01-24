namespace Flex.Infrastructures.Messaging.Inbox
{
    /// <summary>
    /// Result of message consumption indicating how RabbitMQ should handle the message.
    /// </summary>
    public enum ConsumeResult
    {
        /// <summary>
        /// Message processed successfully - ACK to RabbitMQ
        /// </summary>
        Ack,

        /// <summary>
        /// Transient error - NACK with requeue for retry
        /// </summary>
        Retry,

        /// <summary>
        /// Permanent error - NACK without requeue (send to DLQ)
        /// </summary>
        DeadLetter
    }
}
