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
        /// Message is currently being processed (PR).
        /// </summary>
        public const string Processing = "PR";

        /// <summary>
        /// Message has been successfully processed and published (PD).
        /// </summary>
        public const string Processed = "PD";

        /// <summary>
        /// Message processing failed but can be retried (F).
        /// </summary>
        public const string Failed = "F";

        /// <summary>
        /// Message processing permanently failed after max retries (PF).
        /// </summary>
        public const string PermanentlyFailed = "PF";
    }
}
