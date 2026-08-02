using Microsoft.EntityFrameworkCore;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Disciplinary;

public class DisciplinaryDashboardQueryRepository : IDisciplinaryDashboardQueryRepository
{
    private readonly SighDbContext _context;

    public DisciplinaryDashboardQueryRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<DisciplinaryDashboardDto> GetDashboardAsync(
        GetDisciplinaryDashboardQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default)
    {
        var casesQuery = _context.DisciplinaryCases
            .AsNoTracking()
            .Where(c => c.CompanyId == authorizedCompanyId && !c.IsDeleted);

        if (query.DateFrom.HasValue)
        {
            casesQuery = casesQuery.Where(c => c.OpenedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            casesQuery = casesQuery.Where(c => c.OpenedAt <= query.DateTo.Value);
        }

        if (query.Status.HasValue)
        {
            casesQuery = casesQuery.Where(c => c.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            casesQuery = casesQuery.Where(c => c.Priority == query.Priority.Value);
        }

        if (query.ResponsibleEmployeeId.HasValue)
        {
            casesQuery = casesQuery.Where(c => c.ResponsibleEmployeeId == query.ResponsibleEmployeeId.Value);
        }

        var caseList = await casesQuery
            .Include(c => c.Occurrences.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.InfractionType)
            .Include(c => c.Measures.Where(m => !m.IsDeleted))
            .Include(c => c.Employees.Where(e => !e.IsDeleted))
            .ToListAsync(cancellationToken);

        int totalCases = caseList.Count;
        if (totalCases == 0)
        {
            return new DisciplinaryDashboardDto();
        }

        int draftCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.Draft);
        int openCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.Open);
        int underInvestigationCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.UnderInvestigation);
        int awaitingDecisionCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision);
        int decidedCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.Decided);
        int concludedCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.Completed);
        int cancelledCases = caseList.Count(c => c.Status == DisciplinaryCaseStatus.Cancelled);

        var concludedItems = caseList.Where(c => c.Status == DisciplinaryCaseStatus.Completed && c.ClosedAt.HasValue).ToList();
        double avgResolutionDays = 0;
        if (concludedItems.Count > 0)
        {
            avgResolutionDays = concludedItems.Average(c => (c.ClosedAt!.Value - c.OpenedAt).TotalDays);
        }

        int casesOpenedInPeriod = totalCases;
        int casesConcludedInPeriod = concludedCases;

        int employeesWithCases = caseList
            .SelectMany(c => c.Employees)
            .Select(e => e.EmployeeId)
            .Distinct()
            .Count();

        int measuresApplied = caseList
            .SelectMany(c => c.Measures)
            .Count();

        // 1. CasesByStatus
        var casesByStatus = caseList
            .GroupBy(c => c.Status.ToString())
            .Select(g => new DashboardDistributionItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Percentage = Math.Round((g.Count() / (double)totalCases) * 100, 2)
            })
            .ToList();

        // 2. CasesByPriority
        var casesByPriority = caseList
            .GroupBy(c => c.Priority.ToString())
            .Select(g => new DashboardDistributionItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Percentage = Math.Round((g.Count() / (double)totalCases) * 100, 2)
            })
            .ToList();

        // 3. CasesByMonth
        var casesByMonth = caseList
            .GroupBy(c => c.OpenedAt.ToString("yyyy-MM"))
            .Select(g => new DashboardDistributionItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Percentage = Math.Round((g.Count() / (double)totalCases) * 100, 2)
            })
            .OrderBy(g => g.Label)
            .ToList();

        // 4. CasesByInfractionType
        var infractionOccurrences = caseList
            .SelectMany(c => c.Occurrences)
            .Where(o => o.InfractionType != null && !o.InfractionType.IsDeleted)
            .ToList();

        int totalInfractions = infractionOccurrences.Count;
        var casesByInfractionType = infractionOccurrences
            .GroupBy(o => new { o.InfractionTypeId, Name = o.InfractionType?.Name ?? "Outro" })
            .Select(g => new DashboardDistributionItemDto
            {
                Id = g.Key.InfractionTypeId.ToString(),
                Label = g.Key.Name,
                Value = g.Count(),
                Percentage = totalInfractions > 0 ? Math.Round((g.Count() / (double)totalInfractions) * 100, 2) : 0
            })
            .ToList();

        // 5. MeasuresByType
        var allMeasures = caseList.SelectMany(c => c.Measures).ToList();
        int totalMeasuresCount = allMeasures.Count;
        var measuresByType = allMeasures
            .GroupBy(m => m.MeasureType.ToString())
            .Select(g => new DashboardDistributionItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Percentage = totalMeasuresCount > 0 ? Math.Round((g.Count() / (double)totalMeasuresCount) * 100, 2) : 0
            })
            .ToList();

        // 6. CasesByDepartment
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == authorizedCompanyId && !d.IsDeleted)
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var employeeIds = caseList.SelectMany(c => c.Employees).Select(e => e.EmployeeId).Distinct().ToList();
        var employeeDeptMap = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted && e.DepartmentId.HasValue)
            .ToDictionaryAsync(e => e.Id, e => e.DepartmentId!.Value, cancellationToken);

        var deptGroupCount = new Dictionary<string, int>();
        foreach (var c in caseList)
        {
            var deptNames = c.Employees
                .Where(e => employeeDeptMap.ContainsKey(e.EmployeeId) && departments.ContainsKey(employeeDeptMap[e.EmployeeId]))
                .Select(e => departments[employeeDeptMap[e.EmployeeId]])
                .Distinct()
                .ToList();

            if (deptNames.Count == 0)
            {
                deptNames.Add("Sem Setor");
            }

            foreach (var dName in deptNames)
            {
                if (!deptGroupCount.ContainsKey(dName)) deptGroupCount[dName] = 0;
                deptGroupCount[dName]++;
            }
        }

        var casesByDepartment = deptGroupCount.Select(kvp => new DashboardDistributionItemDto
        {
            Label = kvp.Key,
            Value = kvp.Value,
            Percentage = Math.Round((kvp.Value / (double)totalCases) * 100, 2)
        }).ToList();

        return new DisciplinaryDashboardDto
        {
            TotalCases = totalCases,
            DraftCases = draftCases,
            OpenCases = openCases,
            UnderInvestigationCases = underInvestigationCases,
            AwaitingDecisionCases = awaitingDecisionCases,
            DecidedCases = decidedCases,
            ConcludedCases = concludedCases,
            CancelledCases = cancelledCases,
            AverageResolutionTimeInDays = Math.Round(avgResolutionDays, 1),
            CasesOpenedInPeriod = casesOpenedInPeriod,
            CasesConcludedInPeriod = casesConcludedInPeriod,
            EmployeesWithCases = employeesWithCases,
            MeasuresApplied = measuresApplied,
            CasesByStatus = casesByStatus,
            CasesByPriority = casesByPriority,
            CasesByMonth = casesByMonth,
            CasesByInfractionType = casesByInfractionType,
            MeasuresByType = measuresByType,
            CasesByDepartment = casesByDepartment
        };
    }
}
