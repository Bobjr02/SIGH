using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.History;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Importação do histórico disciplinar (planilha de penalidades anterior ao SIGH), em duas etapas, até o go-live.
/// Depois de encerrada, pré-visualizar, confirmar e encerrar respondem 409; status e lotes continuam consultáveis.
/// </summary>
[Route("api/v1/disciplinary-history/import")]
[Tags("Disciplinary history import")]
[Authorize]
public class HistoryImportController : BaseController
{
    private readonly IHistoryImportService _service;

    public HistoryImportController(IHistoryImportService service)
    {
        _service = service;
    }

    [HttpGet("status")]
    [Permission(PermissionCodes.DisciplinaryHistory.Import)]
    public async Task<ActionResult<Result<HistoryImportStatusDto>>> Status(CancellationToken cancellationToken) =>
        OkResult(await _service.GetStatusAsync(cancellationToken));

    [HttpGet("batches")]
    [Permission(PermissionCodes.DisciplinaryHistory.Import)]
    public async Task<ActionResult<Result<PagedResult<HistoryImportBatchDto>>>> Batches(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        OkResult(await _service.ListBatchesAsync(pageNumber, pageSize, cancellationToken));

    [HttpPost("preview")]
    [Permission(PermissionCodes.DisciplinaryHistory.Import)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<HistoryImportPreviewDto>>> Preview(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequestResult<HistoryImportPreviewDto>("Envie a planilha .xlsx no campo \"file\".");

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _service.PreviewAsync(stream, file.FileName, GetUserId(), cancellationToken));
    }

    [HttpPost("{batchId:guid}/confirm")]
    [Permission(PermissionCodes.DisciplinaryHistory.Import)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<HistoryImportResultDto>>> Confirm([FromRoute] Guid batchId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequestResult<HistoryImportResultDto>("Envie de novo a mesma planilha da pré-visualização no campo \"file\".");

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _service.ConfirmAsync(batchId, stream, GetUserId(), cancellationToken));
    }

    /// <summary>Encerra a importação (sem reabertura pela tela; só por History:ImportClosed=false). Auditado.</summary>
    [HttpPost("close")]
    [Permission(PermissionCodes.DisciplinaryHistory.Import)]
    public async Task<ActionResult<Result<HistoryImportStatusDto>>> Close(CancellationToken cancellationToken) =>
        ToActionResult(await _service.CloseAsync(GetUserId(), cancellationToken));

    private ActionResult<Result<T>> ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
            return Ok(result);
        return result.ErrorCode switch
        {
            HistoryImportErrors.Closed or HistoryImportErrors.BatchAlreadyApplied or HistoryImportErrors.PlanChanged => Conflict(result),
            HistoryImportErrors.BatchNotFound => NotFound(result),
            _ => BadRequest(result)
        };
    }
}
