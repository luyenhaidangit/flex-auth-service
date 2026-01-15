using Flex.Domain.Entities;
using Flex.Identity.Errors;
using Flex.Identity.Models.Users;
using Flex.Identity.Repositories.Interfaces;
using Flex.Identity.Services.Interfaces;
using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.Events;
using Flex.Infrastructures.Exceptions;
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
        private readonly IDomainEventDispatcher _domainEventDispatcher;

        public AuthService(
            IdentityDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            IUserRepository userRepository,
            IDomainEventDispatcher domainEventDispatcher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _userRepository = userRepository;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public async Task<LoginResult> LoginAsync(
            LoginRequest request, 
            string? ipAddress = null, 
            string? userAgent = null,
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

            // Mark user as logged in (raises domain event)
            // Note: Since GetByUserNameAsync uses AsNoTracking, we need to attach the entity
            // to track domain events. Alternatively, we can dispatch events directly.
            // For now, we'll attach the entity to the context to track domain events.
            _dbContext.Users.Attach(user);
            user.MarkLoggedIn("ONLINE", ipAddress, userAgent);

            // Collect domain events from all tracked entities
            var domainEvents = _dbContext.ChangeTracker.Entries<EntityBase<long>>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            // Save changes (if any) - in this case, we're just tracking for events
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Dispatch domain events after successful save
            if (domainEvents.Any())
            {
                await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            }

            // Clear domain events from entities
            foreach (var entry in _dbContext.ChangeTracker.Entries<EntityBase<long>>())
            {
                entry.Entity.ClearDomainEvents();
            }

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
