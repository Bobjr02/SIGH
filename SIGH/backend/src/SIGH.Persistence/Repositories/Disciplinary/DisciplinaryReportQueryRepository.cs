using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Disciplinary;

public class DisciplinaryReportQueryRepository : IDisciplinaryReportQueryRepository
{
    private readonly SighDbContext _context;

    public DisciplinaryReportQueryRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DisciplinaryMeasureSummaryDto>> GetMeasuresAsync(
        GetDisciplinaryMeasuresQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default)
    {
        var dbQuery =
            from m in _context.DisciplinaryMeasures.AsNoTracking()
            join c in _context.DisciplinaryCases.AsNoTracking()
                on m.DisciplinaryCaseId equals c.Id
            where !m.IsDeleted &&
                  !c.IsDeleted &&
                  c.CompanyId == authorizedCompanyId
            select new { Measure = m, DisciplinaryCase = c };

        if (query.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Measure.EmployeeId == query.EmployeeId.Value);
        }

        if (query.CaseId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Measure.DisciplinaryCaseId == query.CaseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.MeasureType))
        {
            var mType = query.MeasureType.Trim();
            dbQuery = dbQuery.Where(x => x.Measure.MeasureType.ToString() == mType);
        }

        if (query.AppliedFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Measure.AppliedAt >= query.AppliedFrom.Value);
        }

        if (query.AppliedTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Measure.AppliedAt <= query.AppliedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Measure.Reason.ToLower().Contains(term) || x.DisciplinaryCase.CaseNumber.ToLower().Contains(term));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var employeeIds = await dbQuery.Select(x => x.Measure.EmployeeId).Distinct().ToListAsync(cancellationToken);
        var appliedByUserIds = await dbQuery
            .Where(x => x.Measure.AppliedByUserId.HasValue)
            .Select(x => x.Measure.AppliedByUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var employeesMap = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted)
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);

        var appliedByUsersMap = await _context.Users
            .AsNoTracking()
            .Where(u => appliedByUserIds.Contains(u.Id) && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.Measure.AppliedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var resultDtos = items.Select(x => new DisciplinaryMeasureSummaryDto
        {
            MeasureId = x.Measure.Id,
            CaseId = x.Measure.DisciplinaryCaseId,
            CaseNumber = x.DisciplinaryCase.CaseNumber,
            EmployeeId = x.Measure.EmployeeId,
            EmployeeName = employeesMap.TryGetValue(x.Measure.EmployeeId, out var empName) ? empName : "Funcionário",
            MeasureType = x.Measure.MeasureType.ToString(),
            Description = x.Measure.Reason,
            AppliedAt = x.Measure.AppliedAt,
            EffectiveFrom = x.Measure.EffectiveFrom,
            EffectiveTo = x.Measure.EffectiveUntil,
            AppliedByUserId = x.Measure.AppliedByUserId,
            AppliedByUserName = x.Measure.AppliedByUserId.HasValue &&
                                appliedByUsersMap.TryGetValue(x.Measure.AppliedByUserId.Value, out var appName)
                ? appName
                : null
        }).ToList();

        return new PagedResult<DisciplinaryMeasureSummaryDto>(resultDtos, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<PagedResult<DisciplinaryCaseReportItemDto>> GetCasesAsync(
        GetDisciplinaryCaseReportQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = BuildCasesReportQuery(query, authorizedCompanyId);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        dbQuery = ApplySorting(dbQuery, query.SortBy, query.SortDirection);

        var rawItems = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = await ProjectCaseItemsToDtoAsync(rawItems, cancellationToken);

        return new PagedResult<DisciplinaryCaseReportItemDto>(dtos, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<DisciplinaryCaseReportItemDto>> GetCasesForExportAsync(
        ExportDisciplinaryCaseReportCsvQuery query,
        Guid authorizedCompanyId,
        int maximumRecords,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = BuildCasesReportQuery(query, authorizedCompanyId);

        dbQuery = ApplySorting(dbQuery, query.SortBy, query.SortDirection);

        var rawItems = await dbQuery
            .Take(maximumRecords)
            .ToListAsync(cancellationToken);

        return await ProjectCaseItemsToDtoAsync(rawItems, cancellationToken);
    }

    private IQueryable<SIGH.Domain.Disciplinary.Entities.DisciplinaryCase> BuildCasesReportQuery(
        GetDisciplinaryCaseReportQuery query,
        Guid authorizedCompanyId)
    {
        var dbQuery = _context.DisciplinaryCases
            .AsNoTracking()
            .Include(c => c.Occurrences.Where(o => !o.IsDeleted))
            .Include(c => c.Evidences.Where(e => !e.IsDeleted))
            .Include(c => c.Decisions.Where(d => !d.IsDeleted))
            .Include(c => c.Measures.Where(m => !m.IsDeleted))
            .Include(c => c.Employees.Where(e => !e.IsDeleted))
            .Where(c => c.CompanyId == authorizedCompanyId && !c.IsDeleted);

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
            dbQuery = dbQuery.Where(c => c.Employees.Any(e => !e.IsDeleted && e.EmployeeId == query.EmployeeId.Value));
        }

        if (query.InfractionTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Occurrences.Any(o => !o.IsDeleted && o.InfractionTypeId == query.InfractionTypeId.Value));
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
            dbQuery = dbQuery.Where(c => c.CaseNumber.ToLower().Contains(term) || c.Title.ToLower().Contains(term) || c.Description.ToLower().Contains(term));
        }

        return dbQuery;
    }

    private IQueryable<SIGH.Domain.Disciplinary.Entities.DisciplinaryCase> ApplySorting(
        IQueryable<SIGH.Domain.Disciplinary.Entities.DisciplinaryCase> dbQuery,
        string? sortBy,
        string? sortDirection)
    {
        bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLower()) switch
        {
            "casenumber" => isDesc ? dbQuery.OrderByDescending(c => c.CaseNumber) : dbQuery.OrderBy(c => c.CaseNumber),
            "title" => isDesc ? dbQuery.OrderByDescending(c => c.Title) : dbQuery.OrderBy(c => c.Title),
            "status" => isDesc ? dbQuery.OrderByDescending(c => c.Status) : dbQuery.OrderBy(c => c.Status),
            "priority" => isDesc ? dbQuery.OrderByDescending(c => c.Priority) : dbQuery.OrderBy(c => c.Priority),
            "concludedat" => isDesc ? dbQuery.OrderByDescending(c => c.ClosedAt) : dbQuery.OrderBy(c => c.ClosedAt),
            "closedat" => isDesc ? dbQuery.OrderByDescending(c => c.ClosedAt) : dbQuery.OrderBy(c => c.ClosedAt),
            _ => isDesc ? dbQuery.OrderByDescending(c => c.OpenedAt) : dbQuery.OrderBy(c => c.OpenedAt),
        };
    }

    private async Task<List<DisciplinaryCaseReportItemDto>> ProjectCaseItemsToDtoAsync(
        List<SIGH.Domain.Disciplinary.Entities.DisciplinaryCase> cases,
        CancellationToken cancellationToken)
    {
        if (cases.Count == 0) return new List<DisciplinaryCaseReportItemDto>();

        var companyIds = cases.Select(c => c.CompanyId).Distinct().ToList();
        var companies = await _context.Companies
            .AsNoTracking()
            .Where(c => companyIds.Contains(c.Id) && !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, c => c.LegalName ?? c.Name, cancellationToken);

        var employeeIds = cases
            .SelectMany(c => c.Employees.Where(e => !e.IsDeleted).Select(e => e.EmployeeId))
            .Union(cases.Where(c => c.ResponsibleEmployeeId.HasValue).Select(c => c.ResponsibleEmployeeId!.Value))
            .Distinct()
            .ToList();

        var employeesMap = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted)
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        var deptIds = employeesMap.Values.Where(e => e.DepartmentId.HasValue).Select(e => e.DepartmentId!.Value).Distinct().ToList();
        var deptsMap = await _context.Departments
            .AsNoTracking()
            .Where(d => deptIds.Contains(d.Id) && !d.IsDeleted)
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        return cases.Select(c => {
            var responsibleName = c.ResponsibleEmployeeId.HasValue && employeesMap.TryGetValue(c.ResponsibleEmployeeId.Value, out var respEmp)
                ? respEmp.FullName
                : "Não Atribuído";

            var accusedEmp = c.Employees.FirstOrDefault(e => !e.IsDeleted);
            string accusedName = "Sem acusado";
            string deptName = "Geral";

            if (accusedEmp != null && employeesMap.TryGetValue(accusedEmp.EmployeeId, out var accEmp))
            {
                accusedName = accEmp.FullName;
                if (accEmp.DepartmentId.HasValue && deptsMap.TryGetValue(accEmp.DepartmentId.Value, out var dName))
                {
                    deptName = dName;
                }
            }

            double? resDays = c.ClosedAt.HasValue
                ? Math.Round((c.ClosedAt.Value - c.OpenedAt).TotalDays, 1)
                : null;

            return new DisciplinaryCaseReportItemDto
            {
                CaseId = c.Id,
                CaseNumber = c.CaseNumber,
                Title = c.Title,
                CompanyName = companies.TryGetValue(c.CompanyId, out var compName) ? compName : "Empresa",
                DepartmentName = deptName,
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                ResponsibleEmployeeName = responsibleName,
                MainAccusedEmployeeName = accusedName,
                OpenedAt = c.OpenedAt,
                ConcludedAt = c.ClosedAt,
                ResolutionTimeInDays = resDays,
                OccurrencesCount = c.Occurrences.Count(o => !o.IsDeleted),
                EvidencesCount = c.Evidences.Count(e => !e.IsDeleted),
                DecisionsCount = c.Decisions.Count(d => !d.IsDeleted),
                MeasuresCount = c.Measures.Count(m => !m.IsDeleted)
            };
        }).ToList();
    }
}
