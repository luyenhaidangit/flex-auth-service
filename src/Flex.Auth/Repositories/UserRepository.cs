using Flex.Domain.Entities;
using Flex.Identity.Repositories.Interfaces;
using Flex.Infrastructures.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flex.Identity.Repositories
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
            return _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == userName.ToUpper(), ct);
        }
    }
}
