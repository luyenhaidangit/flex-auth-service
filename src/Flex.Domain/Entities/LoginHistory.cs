using Flex.Domain.Abstractions;

namespace Flex.Domain.Entities
{
    /// <summary>
    /// Entity representing a login attempt history record.
    /// This is stored in the database for audit and compliance purposes.
    /// </summary>
    public class LoginHistory : EntityBase<long>
    {
        /// <summary>
        /// User ID who attempted to login.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Username at the time of login.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Login type: ONLINE or TELLER.
        /// </summary>
        public string LoginType { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the client.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Login result: SUCCESS or FAILED.
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Failure reason if login failed.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// When the login attempt occurred (UTC).
        /// </summary>
        public DateTime OccurredOn { get; set; }
    }
}
