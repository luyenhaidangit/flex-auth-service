namespace Flex.Infrastructures.Headers
{
    public static class HeaderNames
    {
        // Observability
        public const string XCorrelationId = "X-Correlation-Id";

        // Removed headers
        public const string Cookie = "Cookie";
        public const string Referer = "Referer";

        // User context
        public const string UserId = "X-User-Id";

        public const string ClientId = "X-Client-Id";
        public const string Channel = "X-Channel";
        public const string Language = "Accept-Language";
        public const string Roles = "X-Roles";
    }
}
