using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Disciplinary;

public class InfractionTypeRepository : IInfractionTypeRepository
{
    private readonly SighDbContext _context;

    public InfractionTypeRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<InfractionType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InfractionTypes
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.InfractionTypes
            .AsNoTracking()
            .AnyAsync(i => i.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(InfractionType infractionType, CancellationToken cancellationToken = default)
    {
        await _context.InfractionTypes.AddAsync(infractionType, cancellationToken);
    }

    public async Task<PagedResult<InfractionTypeDto>> GetPagedAsync(GetInfractionTypesQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.InfractionTypes.AsNoTracking().AsQueryable();

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(i => i.Name.ToLower().Contains(term) || i.Code.ToLower().Contains(term));
        }

        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var page = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await dbQuery
            .OrderBy(i => i.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InfractionTypeDto(
                i.Id,
                i.Code,
                i.Name,
                i.DefaultSeverity,
                i.RequiresFormalInvestigation,
                i.AllowsTerminationRecommendation,
                i.Description,
                i.LegalReference,
                i.IsActive))
            .ToListAsync(cancellationToken);

        return PagedResult<InfractionTypeDto>.Create(items, totalItems, page, pageSize);
    }
}
