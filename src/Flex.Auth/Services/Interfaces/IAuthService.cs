using Flex.Identity.Models.Users;
using System.Security.Claims;

namespace Flex.Identity.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(
            LoginRequest request, 
            string? ipAddress = null, 
            string? userAgent = null,
            CancellationToken ct = default);
        //Task<bool> LogoutAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
        //Task<UserInfo> GetCurrentUserInfoAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}
