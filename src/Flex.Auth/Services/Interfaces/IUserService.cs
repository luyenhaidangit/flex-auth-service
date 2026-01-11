using Flex.Identity.Models.Users;

namespace Flex.Identity.Services.Interfaces
{
    public interface IUserService
    {
        // Command
        Task<long> CreateAsync(CreateUserCommand request);
    }
}
