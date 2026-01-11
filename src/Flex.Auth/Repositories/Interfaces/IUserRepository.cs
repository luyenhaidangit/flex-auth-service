using Flex.Domain.Entities;

namespace Flex.Identity.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default);
        Task<long> CreateAsync(User user, CancellationToken ct = default);
    }
}
