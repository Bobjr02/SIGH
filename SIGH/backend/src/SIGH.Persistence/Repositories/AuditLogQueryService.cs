using Microsoft.EntityFrameworkCore;
using SIGH.Application.Audit;
using SIGH.Application.Common.Models;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly SighDbContext _context;

    public AuditLogQueryService(SighDbContext context)
    {
        _context = context;
    }

    public Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

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

        return PageAsync(query, filter.PageNumber, filter.PageSize, cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>?> GetDisciplinaryCaseTrailAsync(Guid caseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var exists = await _context.DisciplinaryCases.IgnoreQueryFilters().AnyAsync(c => c.Id == caseId, cancellationToken);
        if (!exists)
            return null;

        // Subentidades removidas logicamente também fazem parte da história do processo.
        var ids = new List<Guid> { caseId };
        ids.AddRange(await _context.DisciplinaryOccurrences.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryCaseEmployees.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryEvidences.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryDecisions.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));
        ids.AddRange(await _context.DisciplinaryMeasures.IgnoreQueryFilters().Where(x => x.DisciplinaryCaseId == caseId).Select(x => x.Id).ToListAsync(cancellationToken));

        var idStrings = ids.Select(id => id.ToString()).ToList();
        var query = _context.AuditLogs.AsNoTracking().Where(a => a.EntityId != null && idStrings.Contains(a.EntityId));

        return await PageAsync(query, pageNumber, pageSize, cancellationToken);
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
