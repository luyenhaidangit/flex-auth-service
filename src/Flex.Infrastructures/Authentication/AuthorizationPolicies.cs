namespace Flex.Infrastructures.Authentication
{
    public static class AuthorizationPolicies
    {
        public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireAdminOrUserRole = "RequireAdminOrUserRole";
        public const string RequireEmailVerified = "RequireEmailVerified";
    }
}
