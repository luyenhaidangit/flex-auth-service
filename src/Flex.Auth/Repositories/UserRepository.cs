using Flex.Domain.Entities;
using Flex.Auth.Repositories.Interfaces;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flex.Auth.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _context;

        public UserRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
        {
            var normalizedUserName = userName.ToUpperInvariant();
            var result = _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);
            return result;
        }

        public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default)
        {
            var count = await _context.Users.AsNoTracking()
                .Where(u => u.UserName!.ToLower() == userName.ToLower())
                .CountAsync(ct);

            return count > 0;
        }

        public async Task<long> CreateAsync(User user, CancellationToken ct = default)
        {
            await _context.Users.AddAsync(user, ct);
            await _context.SaveChangesAsync(ct);
            return user.Id;
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync(ct);
        }
    }
}
