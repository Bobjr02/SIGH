using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Medidas disciplinares no fluxo real do RH: o auxiliar REGISTRA a medida já decidida e o encarregado
/// APROVA ou REJEITA. Registrante e aprovador vêm do token. Leituras respeitam o escopo por gerência
/// (fora dele: 404). Anexos usam as evidências do processo:
/// POST /api/v1/disciplinary-cases/{id}/evidences + .../{evidenceId}/file.
/// </summary>
[Route("api/v1/disciplinary-records")]
[Tags("Disciplinary records")]
[Authorize]
public class DisciplinaryRecordsController : BaseController
{
    public const string PossibleDuplicateCode = "POSSIBLE_DUPLICATE";

    private readonly IRecordService _service;
    private readonly IRecordQueries _queries;

    public DisciplinaryRecordsController(IRecordService service, IRecordQueries queries)
    {
        _service = service;
        _queries = queries;
    }

    [HttpGet]
    [Permission(PermissionCodes.DisciplinaryRecords.View)]
    public async Task<ActionResult<Result<PagedResult<RecordSummaryDto>>>> GetRecords([FromQuery] GetRecordsQuery query, CancellationToken cancellationToken) =>
        OkResult(await _queries.GetPagedAsync(query, cancellationToken));

    /// <summary>Opções do formulário e dos filtros (gerências e setores mascarados pelo escopo).</summary>
    [HttpGet("options")]
    [Permission(PermissionCodes.DisciplinaryRecords.View)]
    public async Task<ActionResult<Result<RecordOptionsDto>>> GetOptions(CancellationToken cancellationToken) =>
        OkResult(await _queries.GetOptionsAsync(cancellationToken));

    /// <summary>Quantas medidas o usuário pode aprovar agora (contador do menu).</summary>
    [HttpGet("pending-count")]
    [Permission(PermissionCodes.DisciplinaryRecords.Approve)]
    public async Task<ActionResult<Result<int>>> GetPendingCount(CancellationToken cancellationToken) =>
        OkResult(await _queries.CountPendingForApprovalAsync(cancellationToken));

    [HttpGet("{id:guid}", Name = "GetDisciplinaryRecordById")]
    [Permission(PermissionCodes.DisciplinaryRecords.View)]
    public async Task<ActionResult<Result<RecordDetailDto>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var record = await _queries.GetByIdAsync(id, cancellationToken);
        return record is null ? NotFoundResult<RecordDetailDto>("Medida não encontrada.") : OkResult(record);
    }

    /// <summary>
    /// Registra a medida já decidida (status "Aguardando aprovação"). 409 POSSIBLE_DUPLICATE quando há
    /// medida pendente ou aprovada do mesmo funcionário, tipo e data de ocorrência: reenvie com
    /// confirmPossibleDuplicate = true para confirmar.
    /// </summary>
    [HttpPost]
    [Permission(PermissionCodes.DisciplinaryRecords.Register)]
    public async Task<ActionResult<Result<RecordSaveResult>>> Register([FromBody] RecordInput input, CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(GetUserId(), input, cancellationToken);
        if (result.RequiresDuplicateConfirmation)
            return Conflict(new Result<RecordSaveResult>(false, result, DuplicateMessage, PossibleDuplicateCode));

        return CreatedAtRoute("GetDisciplinaryRecordById", new { id = result.Record!.Summary.Id },
            Result<RecordSaveResult>.Ok(result, "Medida registrada. Aguardando aprovação."));
    }

    /// <summary>Corrige uma medida rejeitada e a reenvia para aprovação (mesmos campos do registro).</summary>
    [HttpPut("{id:guid}")]
    [Permission(PermissionCodes.DisciplinaryRecords.Register)]
    public async Task<ActionResult<Result<RecordSaveResult>>> Correct([FromRoute] Guid id, [FromBody] RecordInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CorrectAsync(GetUserId(), id, input, cancellationToken);
        if (result.RequiresDuplicateConfirmation)
            return Conflict(new Result<RecordSaveResult>(false, result, DuplicateMessage, PossibleDuplicateCode));

        return OkResult(result, "Medida corrigida e reenviada para aprovação.");
    }

    [HttpPost("{id:guid}/approve")]
    [Permission(PermissionCodes.DisciplinaryRecords.Approve)]
    public async Task<ActionResult<Result<RecordDetailDto>>> Approve([FromRoute] Guid id, CancellationToken cancellationToken) =>
        OkResult(await _service.ApproveAsync(GetUserId(), id, cancellationToken), "Medida aprovada.");

    [HttpPost("{id:guid}/reject")]
    [Permission(PermissionCodes.DisciplinaryRecords.Approve)]
    public async Task<ActionResult<Result<RecordDetailDto>>> Reject([FromRoute] Guid id, [FromBody] ReasonInput input, CancellationToken cancellationToken) =>
        OkResult(await _service.RejectAsync(GetUserId(), id, input, cancellationToken), "Medida rejeitada e devolvida ao registrante.");

    [HttpPost("{id:guid}/cancel")]
    [Permission(PermissionCodes.DisciplinaryRecords.Register)]
    public async Task<ActionResult<Result<RecordDetailDto>>> Cancel([FromRoute] Guid id, [FromBody] ReasonInput input, CancellationToken cancellationToken) =>
        OkResult(await _service.CancelAsync(GetUserId(), id, input, cancellationToken), "Medida cancelada.");

    private const string DuplicateMessage =
        "Possível duplicidade: já existe medida pendente ou aprovada do mesmo tipo para este funcionário na mesma data de ocorrência. Confirme para registrar mesmo assim.";
}
