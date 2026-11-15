using Microsoft.EntityFrameworkCore;
using SIGH.Application.Audit;
using SIGH.Application.Common.Models;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly SighDbContext _context;
    private readonly SIGH.Application.Common.Authorization.IAccessScope? _scope;

    public AuditLogQueryService(SighDbContext context, SIGH.Application.Common.Authorization.IAccessScope? scope = null)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        // A trilha geral mistura dados de todas as gerências: só quem tem escopo total a consulta.
        // (Com escopo parcial a lista vem vazia; a trilha de UM processo usa o escopo do processo.)
        if (_scope is not null && !_scope.HasFullAccess)
            query = query.Where(_ => false);

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(a => a.Entity == filter.EntityName.Trim());
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId.Trim());
        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action.Trim());
        if (filter.From.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var ids = await ResolveReferenceAsync(filter.Reference.Trim(), cancellationToken);
            query = query.Where(a => a.EntityId != null && ids.Contains(a.EntityId));
        }

        return await PageAsync(query, filter.PageNumber, filter.PageSize, cancellationToken);
    }

    /// <summary>
    /// "MED-…": a medida e as subentidades dela (como na linha do tempo). Só dígitos: o funcionário com essa
    /// matrícula. Sem correspondência: lista vazia (nunca "tudo").
    /// </summary>
    private async Task<List<string>> ResolveReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        if (reference.StartsWith("MED-", StringComparison.OrdinalIgnoreCase))
        {
            var upper = reference.ToUpperInvariant();
            var caseId = await _context.DisciplinaryCases.IgnoreQueryFilters().Where(c => c.CaseNumber == upper).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
            return caseId is { } id ? (await CaseEntityIdsAsync(id, cancellationToken)).Select(x => x.ToString()).ToList() : [];
        }

        var employeeIds = await _context.Employees.IgnoreQueryFilters().Where(e => e.EmployeeNumber == reference).Select(e => e.Id).ToListAsync(cancellationToken);
        return employeeIds.Select(x => x.ToString()).ToList();
    }

    public async Task<IReadOnlyList<AuditUserOptionDto>> SearchUsersAsync(string? term, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.IgnoreQueryFilters().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(u => u.FullName.Contains(t) || u.Email.Contains(t));
        }

        return await query.OrderBy(u => u.FullName).Take(20)
            .Select(u => new AuditUserOptionDto(u.Id, u.FullName, u.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>?> GetDisciplinaryCaseTrailAsync(Guid caseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var exists = await _context.DisciplinaryCases.IgnoreQueryFilters().AnyAsync(c => c.Id == caseId, cancellationToken);
        if (!exists)
            return null;

        var idStrings = (await CaseEntityIdsAsync(caseId, cancellationToken)).Select(id => id.ToString()).ToList();
        var query = _context.AuditLogs.AsNoTracking().Where(a => a.EntityId != null && idStrings.Contains(a.EntityId));

        return await PageAsync(query, pageNumber, pageSize, cancellationToken);
    }

    /// <summary>O processo e as subentidades dele (inclusive as removidas logicamente).</summary>
    private async Task<List<Guid>> CaseEntityIdsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        // Subentidades removidas logicamente também fazem parte da história do processo.
        var ids = new List<Guid> { caseId };
        ids.AddRange(await _context.DisciplinaryOccurrences.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryCaseEmployees.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryEvidences.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryDecisions.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryMeasures.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        return ids;
    }

    private async Task<PagedResult<AuditLogDto>> PageAsync(IQueryable<AuditLog> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, IAuditLogQueryService.MaxPageSize);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.CreatedAt,
                a.Action,
                a.Entity,
                a.EntityId,
                a.UserId,
                a.User != null ? a.User.FullName : null,
                a.IpAddress,
                a.CorrelationId,
                a.OldValues,
                a.NewValues))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, pageNumber, pageSize, total);
    }
}
