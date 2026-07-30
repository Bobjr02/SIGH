using System.Text;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Options;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Disciplinary.Reports.Utils;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;

namespace SIGH.Application.Disciplinary.Reports.UseCases;

public interface IGetEmployeeDisciplinaryHistoryUseCase
{
    Task<Result<EmployeeDisciplinaryHistoryDto>> ExecuteAsync(GetEmployeeDisciplinaryHistoryQuery query, CancellationToken cancellationToken = default);
}

public class GetEmployeeDisciplinaryHistoryUseCase : IGetEmployeeDisciplinaryHistoryUseCase
{
    private readonly IEmployeeDisciplinaryHistoryQueryRepository _repository;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;

    public GetEmployeeDisciplinaryHistoryUseCase(
        IEmployeeDisciplinaryHistoryQueryRepository repository,
        IAuthorizedCompanyProvider authorizedCompanyProvider)
    {
        _repository = repository;
        _authorizedCompanyProvider = authorizedCompanyProvider;
    }

    public async Task<Result<EmployeeDisciplinaryHistoryDto>> ExecuteAsync(GetEmployeeDisciplinaryHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.EmployeeId == Guid.Empty)
        {
            return Result<EmployeeDisciplinaryHistoryDto>.Failure("O identificador do funcionário é obrigatório.", "INVALID_EMPLOYEE_ID");
        }

        Guid authorizedCompanyId;
        try
        {
            authorizedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(query.CompanyId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<EmployeeDisciplinaryHistoryDto>.Failure("Acesso não autorizado para a empresa informada.", "UNAUTHORIZED_COMPANY");
        }

        if (authorizedCompanyId == Guid.Empty)
        {
            return Result<EmployeeDisciplinaryHistoryDto>.Failure("Nenhuma empresa autorizada foi identificada.", "INVALID_COMPANY_ID");
        }

        var result = await _repository.GetEmployeeHistoryAsync(query, authorizedCompanyId, cancellationToken);
        if (result == null)
        {
            return Result<EmployeeDisciplinaryHistoryDto>.Failure("Funcionário não encontrado ou sem histórico acessível para a empresa informada.", "EMPLOYEE_NOT_FOUND");
        }

        return Result<EmployeeDisciplinaryHistoryDto>.Ok(result);
    }
}

public interface IGetDisciplinaryDashboardUseCase
{
    Task<Result<DisciplinaryDashboardDto>> ExecuteAsync(GetDisciplinaryDashboardQuery query, CancellationToken cancellationToken = default);
}

public class GetDisciplinaryDashboardUseCase : IGetDisciplinaryDashboardUseCase
{
    private readonly IDisciplinaryDashboardQueryRepository _repository;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;

    public GetDisciplinaryDashboardUseCase(
        IDisciplinaryDashboardQueryRepository repository,
        IAuthorizedCompanyProvider authorizedCompanyProvider)
    {
        _repository = repository;
        _authorizedCompanyProvider = authorizedCompanyProvider;
    }

    public async Task<Result<DisciplinaryDashboardDto>> ExecuteAsync(GetDisciplinaryDashboardQuery query, CancellationToken cancellationToken = default)
    {
        Guid authorizedCompanyId;
        try
        {
            authorizedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(query.CompanyId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<DisciplinaryDashboardDto>.Failure("Acesso não autorizado para a empresa informada.", "UNAUTHORIZED_COMPANY");
        }

        if (authorizedCompanyId == Guid.Empty)
        {
            return Result<DisciplinaryDashboardDto>.Failure("Nenhuma empresa autorizada foi identificada.", "INVALID_COMPANY_ID");
        }

        var dashboard = await _repository.GetDashboardAsync(query, authorizedCompanyId, cancellationToken);
        return Result<DisciplinaryDashboardDto>.Ok(dashboard);
    }
}

public interface IGetDisciplinaryMeasuresUseCase
{
    Task<Result<PagedResult<DisciplinaryMeasureSummaryDto>>> ExecuteAsync(GetDisciplinaryMeasuresQuery query, CancellationToken cancellationToken = default);
}

public class GetDisciplinaryMeasuresUseCase : IGetDisciplinaryMeasuresUseCase
{
    private readonly IDisciplinaryReportQueryRepository _repository;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;

    public GetDisciplinaryMeasuresUseCase(
        IDisciplinaryReportQueryRepository repository,
        IAuthorizedCompanyProvider authorizedCompanyProvider)
    {
        _repository = repository;
        _authorizedCompanyProvider = authorizedCompanyProvider;
    }

    public async Task<Result<PagedResult<DisciplinaryMeasureSummaryDto>>> ExecuteAsync(GetDisciplinaryMeasuresQuery query, CancellationToken cancellationToken = default)
    {
        Guid authorizedCompanyId;
        try
        {
            authorizedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(query.CompanyId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<PagedResult<DisciplinaryMeasureSummaryDto>>.Failure("Acesso não autorizado para a empresa informada.", "UNAUTHORIZED_COMPANY");
        }

        if (authorizedCompanyId == Guid.Empty)
        {
            return Result<PagedResult<DisciplinaryMeasureSummaryDto>>.Failure("Nenhuma empresa autorizada foi identificada.", "INVALID_COMPANY_ID");
        }

        if (query.PageNumber < 1) query.PageNumber = 1;
        if (query.PageSize < 1 || query.PageSize > 100) query.PageSize = 10;

        var result = await _repository.GetMeasuresAsync(query, authorizedCompanyId, cancellationToken);
        return Result<PagedResult<DisciplinaryMeasureSummaryDto>>.Ok(result);
    }
}

public interface IGetDisciplinaryCaseReportUseCase
{
    Task<Result<PagedResult<DisciplinaryCaseReportItemDto>>> ExecuteAsync(GetDisciplinaryCaseReportQuery query, CancellationToken cancellationToken = default);
}

public class GetDisciplinaryCaseReportUseCase : IGetDisciplinaryCaseReportUseCase
{
    private readonly IDisciplinaryReportQueryRepository _repository;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CaseNumber", "Title", "OpenedAt", "ConcludedAt", "ClosedAt", "Status", "Priority", "ResolutionTimeInDays", "DepartmentName"
    };

    public GetDisciplinaryCaseReportUseCase(
        IDisciplinaryReportQueryRepository repository,
        IAuthorizedCompanyProvider authorizedCompanyProvider)
    {
        _repository = repository;
        _authorizedCompanyProvider = authorizedCompanyProvider;
    }

    public async Task<Result<PagedResult<DisciplinaryCaseReportItemDto>>> ExecuteAsync(GetDisciplinaryCaseReportQuery query, CancellationToken cancellationToken = default)
    {
        Guid authorizedCompanyId;
        try
        {
            authorizedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(query.CompanyId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<PagedResult<DisciplinaryCaseReportItemDto>>.Failure("Acesso não autorizado para a empresa informada.", "UNAUTHORIZED_COMPANY");
        }

        if (authorizedCompanyId == Guid.Empty)
        {
            return Result<PagedResult<DisciplinaryCaseReportItemDto>>.Failure("Nenhuma empresa autorizada foi identificada.", "INVALID_COMPANY_ID");
        }

        if (query.PageNumber < 1) query.PageNumber = 1;
        if (query.PageSize < 1 || query.PageSize > 100) query.PageSize = 10;

        if (!string.IsNullOrWhiteSpace(query.SortBy) && !AllowedSortFields.Contains(query.SortBy))
        {
            return Result<PagedResult<DisciplinaryCaseReportItemDto>>.Failure($"O campo de ordenação '{query.SortBy}' não é permitido.", "INVALID_SORT_FIELD");
        }

        var result = await _repository.GetCasesAsync(query, authorizedCompanyId, cancellationToken);
        return Result<PagedResult<DisciplinaryCaseReportItemDto>>.Ok(result);
    }
}

public interface IExportDisciplinaryCaseReportCsvUseCase
{
    Task<Result<byte[]>> ExecuteAsync(ExportDisciplinaryCaseReportCsvQuery query, CancellationToken cancellationToken = default);
}

public class ExportDisciplinaryCaseReportCsvUseCase : IExportDisciplinaryCaseReportCsvUseCase
{
    private readonly IDisciplinaryReportQueryRepository _repository;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;
    private readonly DisciplinaryReportOptions _options;

    public ExportDisciplinaryCaseReportCsvUseCase(
        IDisciplinaryReportQueryRepository repository,
        IAuthorizedCompanyProvider authorizedCompanyProvider,
        IOptions<DisciplinaryReportOptions> options)
    {
        _repository = repository;
        _authorizedCompanyProvider = authorizedCompanyProvider;
        _options = options.Value;
    }

    public async Task<Result<byte[]>> ExecuteAsync(ExportDisciplinaryCaseReportCsvQuery query, CancellationToken cancellationToken = default)
    {
        Guid authorizedCompanyId;
        try
        {
            authorizedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(query.CompanyId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<byte[]>.Failure("Acesso não autorizado para a empresa informada.", "UNAUTHORIZED_COMPANY");
        }

        if (authorizedCompanyId == Guid.Empty)
        {
            return Result<byte[]>.Failure("Nenhuma empresa autorizada foi identificada.", "INVALID_COMPANY_ID");
        }

        int maxAllowed = _options.MaximumExportRecords > 0 ? _options.MaximumExportRecords : 10000;
        string sep = !string.IsNullOrEmpty(_options.CsvSeparator) ? _options.CsvSeparator : ";";

        // Query maximumAllowed + 1 to detect limit overrun
        var items = await _repository.GetCasesForExportAsync(query, authorizedCompanyId, maxAllowed + 1, cancellationToken);

        if (items.Count > maxAllowed)
        {
            return Result<byte[]>.Failure("A exportação excede o limite máximo permitido de registros. Refine os filtros e tente novamente.", "EXPORT_LIMIT_EXCEEDED");
        }

        var csvBuilder = new StringBuilder();
        // UTF-8 BOM for Excel compatibility
        csvBuilder.Append('\uFEFF');

        // CSV Header
        csvBuilder.AppendLine($"Número do Processo{sep}Título{sep}Empresa{sep}Setor{sep}Status{sep}Prioridade{sep}Responsável{sep}Acusado Principal{sep}Data de Abertura{sep}Data de Conclusão{sep}Tempo Resolução (Dias){sep}Ocorrências{sep}Provas{sep}Decisões{sep}Medidas");

        foreach (var item in items)
        {
            var line = string.Join(sep,
                CsvSanitizer.SanitizeField(item.CaseNumber, sep),
                CsvSanitizer.SanitizeField(item.Title, sep),
                CsvSanitizer.SanitizeField(item.CompanyName, sep),
                CsvSanitizer.SanitizeField(item.DepartmentName, sep),
                CsvSanitizer.SanitizeField(item.Status, sep),
                CsvSanitizer.SanitizeField(item.Priority, sep),
                CsvSanitizer.SanitizeField(item.ResponsibleEmployeeName, sep),
                CsvSanitizer.SanitizeField(item.MainAccusedEmployeeName, sep),
                item.OpenedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                item.ConcludedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                item.ResolutionTimeInDays?.ToString("F1") ?? "",
                item.OccurrencesCount,
                item.EvidencesCount,
                item.DecisionsCount,
                item.MeasuresCount
            );
            csvBuilder.AppendLine(line);
        }

        var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        return Result<byte[]>.Ok(bytes);
    }
}
