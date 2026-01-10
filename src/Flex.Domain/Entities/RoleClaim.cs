using Microsoft.AspNetCore.Identity;

namespace Flex.Domain.Entities
{
    public class RoleClaim : IdentityRoleClaim<long>
    {
        public string? Description { get; set; } = string.Empty;
    }
}