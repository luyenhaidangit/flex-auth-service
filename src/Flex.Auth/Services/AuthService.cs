using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Domain.Events.Users;
using Flex.Identity.Models.Users;
using Flex.Identity.Repositories.Interfaces;
using Flex.Identity.Services.Interfaces;
using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.Events;
using Flex.Infrastructures.Exceptions;
using Flex.Infrastructures.Http;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using ClaimTypesApp = Flex.Infrastructures.Authentication.ClaimTypes;

namespace Flex.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IRequestContextAccessor _requestContextAccessor;

        public AuthService(
            IdentityDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            IUserRepository userRepository,
            IOutboxWriter outboxWriter,
            IRequestContextAccessor requestContextAccessor)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _userRepository = userRepository;
            _outboxWriter = outboxWriter;
            _requestContextAccessor = requestContextAccessor;
        }

        public async Task<LoginResult> LoginAsync(
            LoginRequest request, 
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
            if (user is null)
            {
                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            // Publish success event to outbox
            var ipAddress = _requestContextAccessor.ClientIp;
            var loginEvent = new UserLoggedInSuccessEvent(
                UserId: user.Id,
                UserName: user.UserName ?? user.Id.ToString(),
                LoginType: LoginHistoryConstants.LoginType.User,
                IpAddress: ipAddress
            );

            await _outboxWriter.AddAsync(loginEvent, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Include standard claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypesApp.Jti,  Guid.NewGuid().ToString()),
                new Claim(ClaimTypesApp.Iss, _jwtSettings.Issuer),
                new Claim(ClaimTypesApp.Aud, _jwtSettings.Audience),
                new Claim(ClaimTypesApp.Sub, user.UserName ?? string.Empty),
                new Claim(ClaimTypesApp.Email, user.Email ?? string.Empty),
            };

            var token = _tokenService.GenerateToken(_jwtSettings, claims);
            var result = new LoginResult(token);

            return result;
        }

        //public async Task<bool> LogoutAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        //{
        //    var jti = user.FindFirstValue(ClaimTypesApp.Jti);
        //    var expClaim = user.FindFirstValue(ClaimTypesApp.Exp);

        //    if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnix))
        //    {
        //        return false;
        //    }

        //    var exp = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        //    var now = DateTime.UtcNow;

        //    if (exp <= now)
        //    {
        //        return true;
        //    }

        //    var ttl = exp - now;
        //    await _jwtBacklistTokenService.RevokeTokenAsync(jti, ttl);
        //    return true;
        //}

        //public async Task<UserInfoResult> GetCurrentUserInfoAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        //{
        //    var userName = user.FindFirstValue(ClaimTypesApp.Sub);
        //    if (string.IsNullOrEmpty(userName))
        //    {
        //        throw new ValidationException(ErrorCode.Unauthorized);
        //    }

        //    var entity = await _userRepository.GetByUserNameAsync(userName, cancellationToken)
        //        ?? throw new ValidationException(ErrorCode.UserNotFound);

        //    var userInfo = new UserInfo
        //    {
        //        UserName = entity.UserName ?? string.Empty,
        //        Email = entity.Email ?? string.Empty
        //    };

        //    return userInfo;
        //}
    }
}
