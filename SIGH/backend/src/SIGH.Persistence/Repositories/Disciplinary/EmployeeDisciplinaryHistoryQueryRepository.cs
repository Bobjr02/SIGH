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
    private readonly SIGH.Application.Common.Authorization.IAccessScope? _scope;

    public EmployeeDisciplinaryHistoryQueryRepository(SighDbContext context, SIGH.Application.Common.Authorization.IAccessScope? scope = null)
    {
        _context = context;
        _scope = scope;
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

        // Fora do escopo por gerência: o funcionário "não existe" (404).
        if (_scope is not null && !(await _scope.GetFilterAsync(cancellationToken)).Allows(employee.ManagementUnitId))
            return null;

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == authorizedCompanyId && !c.IsDeleted, cancellationToken);

        var casesQuery = _context.DisciplinaryCases
            .AsNoTracking()
            .Include(c => c.Occurrences.Where(o => !o.IsDeleted))
            .Include(c => c.Decisions.Where(d => !d.IsDeleted))
            .Include(c => c.Measures.Where(m => !m.IsDeleted));

        var caseEmployeesQuery =
            from ce in _context.DisciplinaryCaseEmployees.AsNoTracking()
            join c in casesQuery on ce.DisciplinaryCaseId equals c.Id
            where ce.EmployeeId == query.EmployeeId &&
                  !ce.IsDeleted &&
                  !c.IsDeleted &&
                  c.CompanyId == authorizedCompanyId
            select c;

        if (query.DateFrom.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(c => c.OpenedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(c => c.OpenedAt <= query.DateTo.Value);
        }

        if (query.Status.HasValue)
        {
            caseEmployeesQuery = caseEmployeesQuery.Where(c => c.Status == query.Status.Value);
        }

        var caseEmployeesList = await caseEmployeesQuery.ToListAsync(cancellationToken);
        var batchIds = caseEmployeesList.Where(c => c.HistoryImportBatchId != null).Select(c => c.HistoryImportBatchId!.Value).Distinct().ToList();
        var batchDates = await _context.DisciplinaryHistoryImportBatches.AsNoTracking()
            .Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.AppliedAt ?? b.PreviewedAt, cancellationToken);

        var casesList = caseEmployeesList.Select(c => {
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
                MeasuresCount = c.Measures.Count(m => !m.IsDeleted),
                Origin = c.Kind == SIGH.Domain.Disciplinary.Enums.DisciplinaryCaseKind.Historical ? "Histórico" : "SIGH",
                ImportedAt = c.HistoryImportBatchId is { } b && batchDates.TryGetValue(b, out var at) ? at : null,
                // Histórico: a data é a da medida (ClosedAt); o OpenedAt é o momento da importação.
                OccurredAt = c.Kind == SIGH.Domain.Disciplinary.Enums.DisciplinaryCaseKind.Historical && c.ClosedAt is { } closed ? closed : c.OpenedAt
            };
        }).OrderByDescending(c => c.OccurredAt).ToList();

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
            CompanyName = company?.LegalName ?? company?.Name ?? "Empresa",
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
