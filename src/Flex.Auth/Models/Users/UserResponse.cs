namespace Flex.Auth.Models.Users
{
    public class UserResponse
    {
        public long Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
