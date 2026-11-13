using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Interfaces;

namespace SIGH.Application.Common.Authorization;

/// <summary>Um processo/medida está no escopo do usuário? (funcionário acusado na gerência, hoje).</summary>
public interface ICaseVisibility
{
    Task<bool> IsVisibleAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>Lança KeyNotFoundException (→ 404) se o processo não existe ou está fora do escopo.</summary>
    Task EnsureVisibleAsync(Guid caseId, CancellationToken cancellationToken = default);
}

public sealed class CaseVisibility : ICaseVisibility
{
    private readonly IApplicationDbContext _db;
    private readonly IAccessScope _scope;

    public CaseVisibility(IApplicationDbContext db, IAccessScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<bool> IsVisibleAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var filter = await _scope.GetFilterAsync(cancellationToken);
        return await _db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.Id == caseId)
            .InScope(_db.DisciplinaryCaseEmployees, _db.Employees, filter)
            .AnyAsync(cancellationToken);
    }

    public async Task EnsureVisibleAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        if (!await IsVisibleAsync(caseId, cancellationToken))
            throw new KeyNotFoundException("Processo disciplinar não encontrado.");
    }
}
