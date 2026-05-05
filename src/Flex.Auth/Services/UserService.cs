using Flex.Domain.Entities;
using Flex.Auth.Models.Users;
using Flex.Auth.Repositories.Interfaces;
using Flex.Auth.Services.Interfaces;
using Flex.Infrastructures.Exceptions;
using Flex.Infrastructures.Random;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Identity;

namespace Flex.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            ILogger<UserService> logger,
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _logger = logger;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        #region Create
        public async Task<long> CreateAsync(CreateUserCommand command)
        {
            await ValidateCreateUserAsync(command);

            // ===== Create new user =====
            var user = new User
            {
                UserName = command.UserName,
                NormalizedUserName = command.UserName.ToUpperInvariant(),
                Email = command.Email,
                NormalizedEmail = command.Email?.ToUpperInvariant(),
                FullName = command.FullName,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            };

            // ===== Generate random password and hash it =====
            var password = RandomPasswordGenerator.Generate(new PasswordGenerationOptions
            {
                Length = 10,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireSpecial = false
            });
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            // ===== Create user using repository =====
            var userId = await _userRepository.CreateAsync(user);
            return userId;
        }

        private async Task ValidateCreateUserAsync(CreateUserCommand command)
        {
            var username = command.UserName.ToLower();

            // ===== Validate request =====
            // Check if user already exists by username
            if (await _userRepository.ExistsByUserNameAsync(username))
            {
                throw new ValidationException(ResponseCode.UserAlreadyExists);
            }
        }
        #endregion

        #region Get All
        public async Task<IEnumerable<UserResponse>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(u => new UserResponse
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FullName = u.FullName,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnabled = u.LockoutEnabled,
                AccessFailedCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd
            });
        }
        #endregion
    }
}
