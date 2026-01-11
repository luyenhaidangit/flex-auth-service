using Flex.Domain.Entities;

namespace Flex.Identity.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
    }
}
