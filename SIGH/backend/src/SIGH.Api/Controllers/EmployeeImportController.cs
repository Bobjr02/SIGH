using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Import;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Importação da base de funcionários (exportação .xlsx do SAP), em duas etapas:
/// 1. POST /api/v1/employees/import/preview (multipart, campo "file"): dry-run. Devolve o resumo e o
///    batchId; nada é gravado em funcionários.
/// 2. POST /api/v1/employees/import/{batchId}/confirm (multipart, campo "file" + confirmMassTermination):
///    reenvia o MESMO arquivo (o servidor não guarda o arquivo; confere o SHA-256 do lote e a validade),
///    recalcula e aplica numa transação, se o plano não mudou desde a pré-visualização.
/// GET /api/v1/employees/import/batches: histórico.
/// </summary>
[Route("api/v1/employees/import")]
[Tags("Employees Import")]
public class EmployeeImportController : BaseController
{
    private readonly IEmployeeImportService _service;

    public EmployeeImportController(IEmployeeImportService service)
    {
        _service = service;
    }

    [HttpPost("preview")]
    [Permission(PermissionCodes.Employees.Import)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Result<EmployeeImportPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<EmployeeImportPreviewDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<EmployeeImportPreviewDto>>> Preview(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequestResult<EmployeeImportPreviewDto>("Envie a planilha .xlsx no campo \"file\".");

        await using var stream = file.OpenReadStream();
        var result = await _service.PreviewAsync(stream, file.FileName, GetUserId(), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{batchId:guid}/confirm")]
    [Permission(PermissionCodes.Employees.Import)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Result<EmployeeImportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<EmployeeImportResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<EmployeeImportResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<EmployeeImportResultDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result<EmployeeImportResultDto>>> Confirm(
        [FromRoute] Guid batchId,
        IFormFile? file,
        [FromForm] bool confirmMassTermination,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequestResult<EmployeeImportResultDto>("Envie de novo a mesma planilha da pré-visualização no campo \"file\".");

        await using var stream = file.OpenReadStream();
        var result = await _service.ConfirmAsync(batchId, stream, confirmMassTermination, GetUserId(), cancellationToken);
        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            EmployeeImportErrors.BatchNotFound => NotFound(result),
            EmployeeImportErrors.BatchAlreadyApplied or EmployeeImportErrors.PlanChanged => Conflict(result),
            EmployeeImportErrors.MassTerminationNotConfirmed => UnprocessableEntity(result),
            _ => BadRequest(result)
        };
    }

    [HttpGet("batches")]
    [Permission(PermissionCodes.Employees.Import)]
    [ProducesResponseType(typeof(Result<PagedResult<EmployeeImportBatchDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<PagedResult<EmployeeImportBatchDto>>>> Batches(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        OkResult(await _service.ListBatchesAsync(pageNumber, pageSize, cancellationToken));
}
