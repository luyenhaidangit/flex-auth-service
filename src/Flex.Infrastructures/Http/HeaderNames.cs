namespace Flex.Infrastructures.Http
{
    public static class HeaderNames
    {
        // Removed headers
        public const string Cookie = "Cookie";
        public const string Referer = "Referer";

        // Client
        public const string XCorrelationId = "X-Correlation-Id";
        public const string XForwardedFor = "X-Forwarded-For";
        public const string XForwardedProto = "X-Forwarded-Proto";

        // User context
        public const string UserId = "X-User-Id";

        public const string ClientId = "X-Client-Id";
        public const string Channel = "X-Channel";
        public const string Language = "Accept-Language";
        public const string Roles = "X-Roles";
    }
}
