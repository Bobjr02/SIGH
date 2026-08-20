using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Disciplinary.Reports.UseCases;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

[ApiController]
[Route("api/v1/disciplinary-reports")]
[Tags("DisciplinaryReports")]
[Authorize]
public class DisciplinaryReportsController : BaseController
{
    private readonly IGetDisciplinaryDashboardUseCase _getDashboardUseCase;
    private readonly IGetDisciplinaryCaseReportUseCase _getCaseReportUseCase;
    private readonly IExportDisciplinaryCaseReportCsvUseCase _exportCaseReportCsvUseCase;
    private readonly IGetDisciplinaryMeasuresUseCase _getMeasuresUseCase;
    private readonly IGetEmployeeDisciplinaryHistoryUseCase _getEmployeeHistoryUseCase;

    public DisciplinaryReportsController(
        IGetDisciplinaryDashboardUseCase getDashboardUseCase,
        IGetDisciplinaryCaseReportUseCase getCaseReportUseCase,
        IExportDisciplinaryCaseReportCsvUseCase exportCaseReportCsvUseCase,
        IGetDisciplinaryMeasuresUseCase getMeasuresUseCase,
        IGetEmployeeDisciplinaryHistoryUseCase getEmployeeHistoryUseCase)
    {
        _getDashboardUseCase = getDashboardUseCase;
        _getCaseReportUseCase = getCaseReportUseCase;
        _exportCaseReportCsvUseCase = exportCaseReportCsvUseCase;
        _getMeasuresUseCase = getMeasuresUseCase;
        _getEmployeeHistoryUseCase = getEmployeeHistoryUseCase;
    }

    /// <summary>
    /// Retorna os indicadores e distribuições consolidada do dashboard disciplinar.
    /// </summary>
    [HttpGet("dashboard")]
    [Permission(DisciplinaryReportPermissions.ViewDashboard)]
    [ProducesResponseType(typeof(Result<DisciplinaryDashboardDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<DisciplinaryDashboardDto>>> GetDashboard(
        [FromQuery] GetDisciplinaryDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getDashboardUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "UNAUTHORIZED_COMPANY")
            {
                return StatusCode(403, new ProblemDetails
                {
                    Title = "Acesso Proibido",
                    Detail = result.Message ?? "Acesso não autorizado para a empresa informada.",
                    Status = 403
                });
            }
            return BadRequestResult<DisciplinaryDashboardDto>(result.Message ?? "Erro ao carregar o dashboard.", result.ErrorCode);
        }
        return OkResult(result.Data!);
    }

    /// <summary>
    /// Consulta analítica paginada de processos disciplinares.
    /// </summary>
    [HttpGet("cases")]
    [Permission(DisciplinaryReportPermissions.ViewCases)]
    [ProducesResponseType(typeof(Result<PagedResult<DisciplinaryCaseReportItemDto>>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<PagedResult<DisciplinaryCaseReportItemDto>>>> GetCasesReport(
        [FromQuery] GetDisciplinaryCaseReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getCaseReportUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "UNAUTHORIZED_COMPANY")
            {
                return StatusCode(403, new ProblemDetails
                {
                    Title = "Acesso Proibido",
                    Detail = result.Message ?? "Acesso não autorizado para a empresa informada.",
                    Status = 403
                });
            }
            return BadRequestResult<PagedResult<DisciplinaryCaseReportItemDto>>(result.Message ?? "Erro ao consultar relatório de processos.", result.ErrorCode);
        }
        return OkResult(result.Data!);
    }

    /// <summary>
    /// Exporta o relatório analítico de processos disciplinares em formato CSV UTF-8.
    /// </summary>
    [HttpGet("cases/export")]
    [Permission(DisciplinaryReportPermissions.Export)]
    [ProducesResponseType(typeof(FileResult), 200, "text/csv")]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> ExportCasesReport(
        [FromQuery] ExportDisciplinaryCaseReportCsvQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _exportCaseReportCsvUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "UNAUTHORIZED_COMPANY")
            {
                return StatusCode(403, new ProblemDetails
                {
                    Title = "Acesso Proibido",
                    Detail = result.Message ?? "Acesso não autorizado para a empresa informada.",
                    Status = 403
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = result.ErrorCode == "EXPORT_LIMIT_EXCEEDED" ? "Limite de Exportação Excedido" : "Erro na Exportação",
                Detail = result.Message ?? "Falha ao gerar o arquivo CSV.",
                Status = 400
            });
        }

        var fileName = $"disciplinary-cases-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(result.Data!, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// Consulta paginada de medidas disciplinares aplicadas.
    /// </summary>
    [HttpGet("measures")]
    [Permission(DisciplinaryReportPermissions.ViewMeasures)]
    [ProducesResponseType(typeof(Result<PagedResult<DisciplinaryMeasureSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<PagedResult<DisciplinaryMeasureSummaryDto>>>> GetMeasuresReport(
        [FromQuery] GetDisciplinaryMeasuresQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getMeasuresUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "UNAUTHORIZED_COMPANY")
            {
                return StatusCode(403, new ProblemDetails
                {
                    Title = "Acesso Proibido",
                    Detail = result.Message ?? "Acesso não autorizado para a empresa informada.",
                    Status = 403
                });
            }
            return BadRequestResult<PagedResult<DisciplinaryMeasureSummaryDto>>(result.Message ?? "Erro ao consultar medidas disciplinares.", result.ErrorCode);
        }
        return OkResult(result.Data!);
    }

    /// <summary>
    /// Consulta o histórico disciplinar consolidado de um funcionário específico.
    /// </summary>
    [HttpGet("employees/{employeeId:guid}/history")]
    [Permission(DisciplinaryReportPermissions.ViewEmployeeHistory)]
    [ProducesResponseType(typeof(Result<EmployeeDisciplinaryHistoryDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<Result<EmployeeDisciplinaryHistoryDto>>> GetEmployeeHistory(
        [FromRoute] Guid employeeId,
        [FromQuery] GetEmployeeDisciplinaryHistoryQuery query,
        CancellationToken cancellationToken)
    {
        query.EmployeeId = employeeId;
        var result = await _getEmployeeHistoryUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "UNAUTHORIZED_COMPANY")
            {
                return StatusCode(403, new ProblemDetails
                {
                    Title = "Acesso Proibido",
                    Detail = result.Message ?? "Acesso não autorizado para a empresa informada.",
                    Status = 403
                });
            }

            if (result.ErrorCode == "EMPLOYEE_NOT_FOUND")
            {
                return NotFoundResult<EmployeeDisciplinaryHistoryDto>(result.Message ?? "Funcionário não encontrado.", result.ErrorCode);
            }
            return BadRequestResult<EmployeeDisciplinaryHistoryDto>(result.Message ?? "Erro ao consultar histórico do funcionário.", result.ErrorCode);
        }
        return OkResult(result.Data!);
    }
}
