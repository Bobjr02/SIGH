using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Disciplinary;

public class EmployeeDisciplinaryHistoryQueryRepository : IEmployeeDisciplinaryHistoryQueryRepository
{
    private readonly SighDbContext _context;

    public EmployeeDisciplinaryHistoryQueryRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDisciplinaryHistoryDto?> GetEmployeeHistoryAsync(
        GetEmployeeDisciplinaryHistoryQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EmployeeId && e.CompanyId == authorizedCompanyId && !e.IsDeleted, cancellationToken);

        if (employee == null) return null;

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == authorizedCompanyId && !c.IsDeleted, cancellationToken);

        var caseEmployeesQuery = _context.DisciplinaryCaseEmployees
            .AsNoTracking()
            .Include(ce => ce.DisciplinaryCase)
                .ThenInclude(c => c.Occurrences.Where(o => !o.IsDeleted))
            .Include(ce => ce.DisciplinaryCase)
                .ThenInclude(c => c.Decisions.Where(d => !d.IsDeleted))
            .Include(ce => ce.DisciplinaryCase)
                .ThenInclude(c => c.Measures.Where(m => !m.IsDeleted))
            .Where(ce => ce.EmployeeId == query.EmployeeId && !ce.IsDeleted && ce.DisciplinaryCase != null && !ce.DisciplinaryCase.IsDeleted && ce.DisciplinaryCase.CompanyId == authorizedCompanyId);

        if (query.DateFrom.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(ce => ce.DisciplinaryCase.OpenedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(ce => ce.DisciplinaryCase.OpenedAt <= query.DateTo.Value);
        }

        if (query.Status.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(ce => ce.DisciplinaryCase.Status == query.Status.Value);
        }

        var caseEmployeesList = await caseEmployeesQuery.ToListAsync(cancellationToken);

        var casesList = caseEmployeesList.Select(ce => {
            var c = ce.DisciplinaryCase;
            return new EmployeeDisciplinaryCaseSummaryDto
            {
                CaseId = c.Id,
                CaseNumber = c.CaseNumber,
                Title = c.Title,
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                OpenedAt = c.OpenedAt,
                ConcludedAt = c.ClosedAt,
                ResponsibleEmployee = c.ResponsibleEmployeeId?.ToString() ?? "Não atribuído",
                OccurrencesCount = c.Occurrences.Count(o => !o.IsDeleted),
                DecisionsCount = c.Decisions.Count(d => !d.IsDeleted),
                MeasuresCount = c.Measures.Count(m => !m.IsDeleted)
            };
        }).OrderByDescending(c => c.OpenedAt).ToList();

        var totalMeasures = casesList.Sum(c => c.MeasuresCount);
        var totalCases = casesList.Count;
        var openCases = casesList.Count(c => c.Status != "Concluded" && c.Status != "Cancelled" && c.Status != "Concluido" && c.Status != "Cancelado");
        var concludedCases = casesList.Count(c => c.Status == "Concluded" || c.Status == "Concluido");
        var cancelledCases = casesList.Count(c => c.Status == "Cancelled" || c.Status == "Cancelado");

        return new EmployeeDisciplinaryHistoryDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            RegistrationNumber = employee.EmployeeNumber,
            CompanyId = authorizedCompanyId,
            CompanyName = company?.CorporateName ?? company?.TradeName ?? "Empresa",
            TotalCases = totalCases,
            OpenCases = openCases,
            ConcludedCases = concludedCases,
            CancelledCases = cancelledCases,
            TotalMeasures = totalMeasures,
            MostRecentCaseDate = casesList.FirstOrDefault()?.OpenedAt,
            Cases = casesList
        };
    }
}
