using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Flex.Domain.Events.Users;
using Flex.Auth.Models.Users;
using Flex.Auth.Repositories.Interfaces;
using Flex.Auth.Services.Interfaces;
using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Exceptions;
using Flex.Infrastructures.Http;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using ClaimTypesApp = Flex.Infrastructures.Authentication.ClaimTypes;

namespace Flex.Auth.Services
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

        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            // Prepare data
            var ipAddress = _requestContextAccessor.ClientIp;

            // Validate user exists
            var user = await _userRepository.GetByUserNameAsync(request.UserName, ct);
            if (user is null)
            {
                // Publish FAILED event
                var failedEvent = new UserLoginAttemptedEvent(
                    UserId: null,
                    UserName: request.UserName,
                    LoginType: LoginHistoryConstants.LoginType.User,
                    IpAddress: ipAddress,
                    IsSuccess: false
                );
                await _outboxWriter.AddAsync(failedEvent, ct);

                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            // Validate password exists
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                var failedEvent = new UserLoginAttemptedEvent(
                    UserId: user.Id,
                    UserName: user.UserName,
                    LoginType: LoginHistoryConstants.LoginType.User,
                    IpAddress: ipAddress,
                    IsSuccess: false
                );
                await _outboxWriter.AddAsync(failedEvent, ct);

                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            // Verify password
            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                var failedEvent = new UserLoginAttemptedEvent(
                    UserId: user.Id,
                    UserName: user.UserName,
                    LoginType: LoginHistoryConstants.LoginType.User,
                    IpAddress: ipAddress,
                    IsSuccess: false
                );
                await _outboxWriter.AddAsync(failedEvent, ct);

                throw new ValidationException(ResponseCode.InvalidCredentials);
            }

            // Publish SUCCESS event
            var successEvent = new UserLoginAttemptedEvent(
                UserId: user.Id,
                UserName: user.UserName,
                LoginType: LoginHistoryConstants.LoginType.User,
                IpAddress: ipAddress,
                IsSuccess: true
            );
            await _outboxWriter.AddAsync(successEvent, ct);
            await _dbContext.SaveChangesAsync(ct);

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
