using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Interfaces;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

/// <summary>
/// Escopo do usuário da requisição. Sem usuário autenticado o escopo é vazio (nada visível).
/// Resolvido uma vez por requisição (serviço scoped).
/// </summary>
public sealed class AccessScope : IAccessScope
{
    private readonly ICurrentUserService _currentUser;
    private readonly SighDbContext _context;
    private readonly IDateTimeProvider _clock;
    private AccessScopeFilter? _filter;

    public AccessScope(ICurrentUserService currentUser, SighDbContext context, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _context = context;
        _clock = clock;
    }

    public bool HasFullAccess => _currentUser.HasPermission(PermissionCodes.DisciplinaryRecords.ViewAll);

    public async Task<IReadOnlyCollection<Guid>> GetManagementUnitIdsAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not { } userId)
            return Array.Empty<Guid>();

        var now = _clock.UtcNow;
        return await _context.UserManagementUnits.AsNoTracking()
            .Where(x => x.UserId == userId
                        && (x.ValidFrom == null || x.ValidFrom <= now)
                        && (x.ValidUntil == null || x.ValidUntil > now))
            .Select(x => x.ManagementUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<AccessScopeFilter> GetFilterAsync(CancellationToken cancellationToken = default)
    {
        if (_filter is not null)
            return _filter;

        if (!_currentUser.IsAuthenticated)
            return _filter = AccessScopeFilter.Nothing;

        return _filter = HasFullAccess
            ? AccessScopeFilter.Everything
            : new AccessScopeFilter(false, await GetManagementUnitIdsAsync(cancellationToken));
    }
}
