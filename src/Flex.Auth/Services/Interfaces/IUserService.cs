using Flex.Auth.Models.Users;

namespace Flex.Auth.Services.Interfaces
{
    public interface IUserService
    {
        // Command
        Task<long> CreateAsync(CreateUserCommand request);
        
        // Query
        Task<IEnumerable<UserResponse>> GetAllAsync();
    }
}
