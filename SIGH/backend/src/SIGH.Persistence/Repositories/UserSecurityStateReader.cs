using Microsoft.EntityFrameworkCore;
using SIGH.Application.Security;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public sealed class UserSecurityStateReader : IUserSecurityStateReader
{
    private readonly SighDbContext _context;

    public UserSecurityStateReader(SighDbContext context)
    {
        _context = context;
    }

    public Task<UserSecurityState?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSecurityState(u.SecurityStamp, u.Status))
            .FirstOrDefaultAsync(cancellationToken);
}
