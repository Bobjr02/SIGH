using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Queries;

namespace SIGH.Application.Interfaces.Repositories.Disciplinary;

public interface IDisciplinaryDashboardQueryRepository
{
    Task<DisciplinaryDashboardDto> GetDashboardAsync(
        GetDisciplinaryDashboardQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default);
}

public interface IDisciplinaryReportQueryRepository
{
    Task<PagedResult<DisciplinaryCaseReportItemDto>> GetCasesAsync(
        GetDisciplinaryCaseReportQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DisciplinaryMeasureSummaryDto>> GetMeasuresAsync(
        GetDisciplinaryMeasuresQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DisciplinaryCaseReportItemDto>> GetCasesForExportAsync(
        ExportDisciplinaryCaseReportCsvQuery query,
        Guid authorizedCompanyId,
        int maximumRecords,
        CancellationToken cancellationToken = default);
}

public interface IEmployeeDisciplinaryHistoryQueryRepository
{
    Task<EmployeeDisciplinaryHistoryDto?> GetEmployeeHistoryAsync(
        GetEmployeeDisciplinaryHistoryQuery query,
        Guid authorizedCompanyId,
        CancellationToken cancellationToken = default);
}
