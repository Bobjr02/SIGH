using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Entities;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SighDbContext _context;

    public UserRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByEmailWithRolesAndPermissionsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var normalizedCpf = cpf.Trim();
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Cpf == normalizedCpf, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task AddUserSessionAsync(UserSession userSession, CancellationToken cancellationToken = default)
    {
        await _context.UserSessions.AddAsync(userSession, cancellationToken);
    }

    public async Task AddPasswordHistoryAsync(PasswordHistory passwordHistory, CancellationToken cancellationToken = default)
    {
        await _context.PasswordHistories.AddAsync(passwordHistory, cancellationToken);
    }

    public async Task<List<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Distinct().ToList();
        return await _context.Roles
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserSession>> GetActiveUserSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(us => us.UserId == userId && !us.IsRevoked)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSession?> GetUserSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(us => us.Id == sessionId && us.UserId == userId, cancellationToken);
    }

    public async Task<List<PasswordHistory>> GetRecentPasswordHistoriesAsync(Guid userId, int take, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
