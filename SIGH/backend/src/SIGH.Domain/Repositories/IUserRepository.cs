using SIGH.Domain.Entities;

namespace SIGH.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithRolesAndPermissionsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task AddUserSessionAsync(UserSession userSession, CancellationToken cancellationToken = default);
    Task AddPasswordHistoryAsync(PasswordHistory passwordHistory, CancellationToken cancellationToken = default);
    Task<List<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserSession>> GetActiveUserSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetUserSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<PasswordHistory>> GetRecentPasswordHistoriesAsync(Guid userId, int take, CancellationToken cancellationToken = default);
}
