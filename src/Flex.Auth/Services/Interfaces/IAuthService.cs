using Flex.Auth.Models.Users;
using System.Security.Claims;

namespace Flex.Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(
            LoginRequest request, 
            CancellationToken ct = default);
        Task<bool> LogoutAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<UserInfo?> GetCurrentUserInfoAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}
