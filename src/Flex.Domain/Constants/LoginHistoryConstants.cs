namespace Flex.Domain.Constants
{
    /// <summary>
    /// Constants for LoginHistory entity and related events.
    /// </summary>
    public static class LoginHistoryConstants
    {
        /// <summary>
        /// Login type constants.
        /// </summary>
        public static class LoginType
        {
            /// <summary>
            /// User login type.
            /// </summary>
            public const string User = "USER";

            /// <summary>
            /// Service login type.
            /// </summary>
            public const string Service = "SERVICE";

            /// <summary>
            /// System login type.
            /// </summary>
            public const string System = "SYSTEM";

            /// <summary>
            /// Admin login type.
            /// </summary>
            public const string Admin = "ADMIN";
        }

        /// <summary>
        /// Login result constants.
        /// </summary>
        public static class Result
        {
            /// <summary>
            /// Successful login.
            /// </summary>
            public const string Success = "Y";

            /// <summary>
            /// Failed login.
            /// </summary>
            public const string Failed = "N";
        }
    }
}
