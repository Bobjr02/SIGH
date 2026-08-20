using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Disciplinary;

public class DisciplinaryCaseRepository : IDisciplinaryCaseRepository
{
    private readonly SighDbContext _context;

    public DisciplinaryCaseRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<DisciplinaryCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryCases
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<DisciplinaryCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryCases
            .AsSplitQuery()
            .Include(c => c.Occurrences)
            .Include(c => c.Employees)
            .Include(c => c.Evidences)
            .Include(c => c.Decisions)
            .Include(c => c.Measures)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCaseNumberAsync(Guid companyId, string caseNumber, CancellationToken cancellationToken = default)
    {
        var trimmedNumber = caseNumber.Trim();
        return await _context.DisciplinaryCases
            .AsNoTracking()
            .AnyAsync(c => c.CompanyId == companyId && c.CaseNumber == trimmedNumber, cancellationToken);
    }

    public async Task AddAsync(DisciplinaryCase disciplinaryCase, CancellationToken cancellationToken = default)
    {
        await _context.DisciplinaryCases.AddAsync(disciplinaryCase, cancellationToken);
    }

    public async Task<PagedResult<DisciplinaryCaseSummaryDto>> GetPagedAsync(GetDisciplinaryCasesQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.DisciplinaryCases
            .AsNoTracking()
            .Include(c => c.Occurrences)
            .Include(c => c.Employees)
            .AsQueryable();

        if (query.CompanyId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.CompanyId == query.CompanyId.Value);
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Priority == query.Priority.Value);
        }

        if (query.ResponsibleEmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.ResponsibleEmployeeId == query.ResponsibleEmployeeId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Employees.Any(e => e.EmployeeId == query.EmployeeId.Value));
        }

        if (query.DateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.OpenedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.OpenedAt <= query.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(c => c.Title.ToLower().Contains(term) ||
                                         c.CaseNumber.ToLower().Contains(term) ||
                                         c.Description.ToLower().Contains(term));
        }

        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var page = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(c => c.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new DisciplinaryCaseSummaryDto(
                c.Id,
                c.CaseNumber,
                c.CompanyId,
                c.Title,
                c.Status,
                c.Priority,
                c.OpenedAt,
                c.OpenedByUserId,
                c.ResponsibleEmployeeId,
                c.DueDate,
                c.ClosedAt,
                c.Occurrences.Count,
                c.Employees.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<DisciplinaryCaseSummaryDto>(items, page, pageSize, totalItems);
    }
}
