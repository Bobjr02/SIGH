using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Evidences;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Arquivo das evidências. Fluxo em dois passos:
/// 1. POST /api/v1/disciplinary-cases/{caseId}/evidences (JSON) cadastra a evidência (metadados);
/// 2. POST .../evidences/{evidenceId}/file (multipart, campo "file") anexa o arquivo, uma única vez.
/// Download: GET .../evidences/{evidenceId}/file. Remoção lógica: DELETE .../evidences/{evidenceId}.
/// </summary>
[Route("api/v1/disciplinary-cases/{caseId:guid}/evidences")]
[Tags("Disciplinary Case Evidences")]
public class DisciplinaryCaseEvidencesController : BaseController
{
    public record RemoveEvidenceHttpRequest(string Reason);

    /// <summary>Anexa o arquivo à evidência. Formatos: pdf, jpg/jpeg, png, webp, mp4, mp3, m4a, docx, xlsx (validados pela assinatura).</summary>
    [HttpPost("{evidenceId:guid}/file")]
    [Permission(PermissionCodes.DisciplinaryCases.AddEvidence)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Result<EvidenceFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<EvidenceFileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<EvidenceFileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<EvidenceFileDto>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(Result<EvidenceFileDto>), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<Result<EvidenceFileDto>>> Upload(
        [FromRoute] Guid caseId,
        [FromRoute] Guid evidenceId,
        IFormFile? file,
        [FromServices] IUploadEvidenceFileUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequestResult<EvidenceFileDto>("Envie o arquivo no campo \"file\" (multipart/form-data).");

        await using var stream = file.OpenReadStream();
        var result = await useCase.ExecuteAsync(
            new UploadEvidenceFileRequest(caseId, evidenceId, stream, file.FileName, file.Length, GetUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Baixa o arquivo em stream. Cada download é registrado na auditoria.</summary>
    [HttpGet("{evidenceId:guid}/file")]
    [Permission(PermissionCodes.DisciplinaryCases.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<EvidenceDownload>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        [FromRoute] Guid caseId,
        [FromRoute] Guid evidenceId,
        [FromServices] IDownloadEvidenceFileUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(caseId, evidenceId, GetUserId(), cancellationToken);
        if (!result.Success)
            return NotFound(Result<object>.Failure(result.Message!, result.ErrorCode));

        var download = result.Data!;
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-SHA256"] = download.Sha256Hash;

        // File(..., fileDownloadName) gera Content-Disposition: attachment; filename=...; filename*=UTF-8''...
        return File(download.Content, download.ContentType, download.FileName, enableRangeProcessing: false);
    }

    /// <summary>Remoção lógica da evidência. Motivo obrigatório; quem removeu vem do token.</summary>
    [HttpDelete("{evidenceId:guid}")]
    [Permission(PermissionCodes.DisciplinaryCases.RemoveEvidence)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<bool>>> Remove(
        [FromRoute] Guid caseId,
        [FromRoute] Guid evidenceId,
        [FromBody] RemoveEvidenceHttpRequest request,
        [FromServices] IRemoveEvidenceUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(caseId, evidenceId, request.Reason, GetUserId(), cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<Result<T>> ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            EvidenceErrors.CaseNotFound or EvidenceErrors.EvidenceNotFound or EvidenceErrors.FileNotFound => NotFound(result),
            EvidenceErrors.FileTooLarge => StatusCode(StatusCodes.Status413PayloadTooLarge, result),
            EvidenceErrors.UnsupportedFile => StatusCode(StatusCodes.Status415UnsupportedMediaType, result),
            EvidenceErrors.Malware => UnprocessableEntity(result),
            _ => BadRequest(result)
        };
    }
}
