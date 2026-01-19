namespace Flex.Domain.Constants
{
    /// <summary>
    /// Constants for OutboxMessage processing status.
    /// Uses short codes to save database storage space.
    /// </summary>
    public static class OutboxMessageStatus
    {
        /// <summary>
        /// Message is pending processing (P).
        /// </summary>
        public const string Pending = "P";

        /// <summary>
        /// Message has been successfully sent (S).
        /// </summary>
        public const string Sent = "S";

        /// <summary>
        /// Message processing failed but can be retried (F).
        /// </summary>
        public const string Failed = "F";

        /// <summary>
        /// Message processing dead after max retries exceeded (D).
        /// </summary>
        public const string Dead = "D";
    }
}
