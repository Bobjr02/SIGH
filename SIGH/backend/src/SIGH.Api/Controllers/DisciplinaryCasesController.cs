using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Api.Contracts.Disciplinary;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;
using SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;
using SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/v1/disciplinary-cases")]
[Tags("DisciplinaryCases")]
[Authorize]
public class DisciplinaryCasesController : BaseController
{
    private readonly ICreateDisciplinaryCaseUseCase _createDisciplinaryCaseUseCase;
    private readonly IGetDisciplinaryCaseByIdUseCase _getDisciplinaryCaseByIdUseCase;
    private readonly IGetDisciplinaryCasesUseCase _getDisciplinaryCasesUseCase;
    private readonly IOpenCaseUseCase _openCaseUseCase;
    private readonly IStartInvestigationUseCase _startInvestigationUseCase;
    private readonly ISubmitCaseForDecisionUseCase _submitCaseForDecisionUseCase;
    private readonly IApproveDecisionUseCase _approveDecisionUseCase;
    private readonly IRejectDecisionUseCase _rejectDecisionUseCase;
    private readonly IAddOccurrenceUseCase _addOccurrenceUseCase;
    private readonly IAddEmployeeToCaseUseCase _addEmployeeToCaseUseCase;
    private readonly IAddEvidenceUseCase _addEvidenceUseCase;
    private readonly IRecordDecisionUseCase _recordDecisionUseCase;
    private readonly IApplyMeasureUseCase _applyMeasureUseCase;
    private readonly ICancelDisciplinaryCaseUseCase _cancelDisciplinaryCaseUseCase;
    private readonly IConcludeDisciplinaryCaseUseCase _concludeDisciplinaryCaseUseCase;

    public DisciplinaryCasesController(
        ICreateDisciplinaryCaseUseCase createDisciplinaryCaseUseCase,
        IGetDisciplinaryCaseByIdUseCase getDisciplinaryCaseByIdUseCase,
        IGetDisciplinaryCasesUseCase getDisciplinaryCasesUseCase,
        IOpenCaseUseCase openCaseUseCase,
        IStartInvestigationUseCase startInvestigationUseCase,
        ISubmitCaseForDecisionUseCase submitCaseForDecisionUseCase,
        IApproveDecisionUseCase approveDecisionUseCase,
        IRejectDecisionUseCase rejectDecisionUseCase,
        IAddOccurrenceUseCase addOccurrenceUseCase,
        IAddEmployeeToCaseUseCase addEmployeeToCaseUseCase,
        IAddEvidenceUseCase addEvidenceUseCase,
        IRecordDecisionUseCase recordDecisionUseCase,
        IApplyMeasureUseCase applyMeasureUseCase,
        ICancelDisciplinaryCaseUseCase cancelDisciplinaryCaseUseCase,
        IConcludeDisciplinaryCaseUseCase concludeDisciplinaryCaseUseCase)
    {
        _createDisciplinaryCaseUseCase = createDisciplinaryCaseUseCase;
        _getDisciplinaryCaseByIdUseCase = getDisciplinaryCaseByIdUseCase;
        _getDisciplinaryCasesUseCase = getDisciplinaryCasesUseCase;
        _openCaseUseCase = openCaseUseCase;
        _startInvestigationUseCase = startInvestigationUseCase;
        _submitCaseForDecisionUseCase = submitCaseForDecisionUseCase;
        _approveDecisionUseCase = approveDecisionUseCase;
        _rejectDecisionUseCase = rejectDecisionUseCase;
        _addOccurrenceUseCase = addOccurrenceUseCase;
        _addEmployeeToCaseUseCase = addEmployeeToCaseUseCase;
        _addEvidenceUseCase = addEvidenceUseCase;
        _recordDecisionUseCase = recordDecisionUseCase;
        _applyMeasureUseCase = applyMeasureUseCase;
        _cancelDisciplinaryCaseUseCase = cancelDisciplinaryCaseUseCase;
        _concludeDisciplinaryCaseUseCase = concludeDisciplinaryCaseUseCase;
    }

    /// <summary>
    /// Criar um novo processo disciplinar
    /// </summary>
    /// <description>Cria um rascunho de processo disciplinar no sistema.</description>
    [HttpPost]
    [Permission(PermissionCodes.DisciplinaryCases.Create)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<CreateDisciplinaryCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CreateDisciplinaryCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<CreateDisciplinaryCaseResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result<CreateDisciplinaryCaseResponse>>> Create(
        [FromBody] CreateDisciplinaryCaseHttpRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = request.ToApplicationRequest(GetUserId());
        var result = await _createDisciplinaryCaseUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToCreatedActionResult(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Obter processo disciplinar por ID
    /// </summary>
    /// <description>Obtém os detalhes completos do processo disciplinar, incluindo ocorrências, envolvidos, evidências, decisões e medidas.</description>
    [HttpGet("/api/v1/DisciplinaryCases/{id:guid}", Name = "GetDisciplinaryCaseById")]
    [HttpGet("/api/v1/disciplinary-cases/{id:guid}")]
    [Permission(PermissionCodes.DisciplinaryCases.View)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<GetDisciplinaryCaseByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GetDisciplinaryCaseByIdResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<GetDisciplinaryCaseByIdResponse>>> GetById(
        [FromRoute] Guid id,
        [FromServices] SIGH.Application.Common.Authorization.ICaseVisibility visibility,
        CancellationToken cancellationToken)
    {
        // Fora do escopo por gerência: 404, como se não existisse.
        if (!await visibility.IsVisibleAsync(id, cancellationToken))
            return NotFoundResult<GetDisciplinaryCaseByIdResponse>("Processo disciplinar não encontrado.");

        var result = await _getDisciplinaryCaseByIdUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Listar processos disciplinares com paginação e filtros
    /// </summary>
    /// <description>Retorna uma lista paginada de processos disciplinares filtrados por empresa, status, prioridade ou termo de busca.</description>
    [HttpGet]
    [Permission(PermissionCodes.DisciplinaryCases.View)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<PagedResult<DisciplinaryCaseSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<PagedResult<DisciplinaryCaseSummaryDto>>>> GetPaged(
        [FromQuery] GetDisciplinaryCasesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getDisciplinaryCasesUseCase.ExecuteAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Abrir processo disciplinar
    /// </summary>
    /// <description>Transiciona o status do processo disciplinar de Rascunho (Draft) para Aberto (Open).</description>
    [HttpPost("{id:guid}/open")]
    [Permission(PermissionCodes.DisciplinaryCases.Open)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<OpenCaseResponse>>> Open(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // Sem corpo: o processo vem da rota e o ator vem exclusivamente do token.
        var applicationRequest = new OpenCaseRequest(id, GetUserId());
        var result = await _openCaseUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Iniciar investigação do processo disciplinar
    /// </summary>
    /// <description>Inicia a fase de investigação de um processo disciplinar aberto.</description>
    [HttpPost("{id:guid}/start-investigation")]
    [Permission(PermissionCodes.DisciplinaryCases.StartInvestigation)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<StartInvestigationResponse>>> StartInvestigation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // Sem corpo: o processo vem da rota e o ator vem exclusivamente do token.
        var applicationRequest = new StartInvestigationRequest(id, GetUserId());
        var result = await _startInvestigationUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Submeter processo disciplinar para decisão
    /// </summary>
    /// <description>Encaminha o processo sob investigação para a etapa de julgamento/decisão.</description>
    [HttpPost("{id:guid}/submit-for-decision")]
    [Permission(PermissionCodes.DisciplinaryCases.SubmitForDecision)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<SubmitCaseForDecisionResponse>>> SubmitForDecision(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // Sem corpo: o processo vem da rota e o ator vem exclusivamente do token.
        var applicationRequest = new SubmitCaseForDecisionRequest(id, GetUserId());
        var result = await _submitCaseForDecisionUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Aprovar decisão proferida no processo disciplinar
    /// </summary>
    /// <description>Aprova a decisão registrada para o processo disciplinar.</description>
    [HttpPost("{id:guid}/approve-decision")]
    [Permission(PermissionCodes.DisciplinaryCases.ApproveDecision)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ApproveDecisionResponse>>> ApproveDecision(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // Sem corpo: o processo vem da rota e o ator vem exclusivamente do token.
        var applicationRequest = new ApproveDecisionRequest(id, GetUserId());
        var result = await _approveDecisionUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Rejeitar decisão proferida no processo disciplinar
    /// </summary>
    /// <description>Rejeita a decisão registrada com justificativa e retorna para análise.</description>
    [HttpPost("{id:guid}/reject-decision")]
    [Permission(PermissionCodes.DisciplinaryCases.RejectDecision)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<RejectDecisionResponse>>> RejectDecision(
        [FromRoute] Guid id,
        [FromBody] RejectDecisionHttpRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = request.ToApplicationRequest(id, GetUserId());
        var result = await _rejectDecisionUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Adicionar ocorrência de infração ao processo disciplinar
    /// </summary>
    /// <description>Registra uma nova ocorrência com tipo de infração, gravidade e data no processo disciplinar.</description>
    [HttpPost("{id:guid}/occurrences")]
    [Permission(PermissionCodes.DisciplinaryCases.AddOccurrence)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<AddOccurrenceResponse>>> AddOccurrence(
        [FromRoute] Guid id,
        [FromBody] AddOccurrenceHttpRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = request.ToApplicationRequest(id, GetUserId());
        var result = await _addOccurrenceUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Associar funcionário (envolvido) ao processo disciplinar
    /// </summary>
    /// <description>Adiciona um funcionário ao processo com papel específico (Acusado, Testemunha, Noticiante, etc.).</description>
    [HttpPost("{id:guid}/employees")]
    [Permission(PermissionCodes.DisciplinaryCases.AddEmployee)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<AddEmployeeToCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddEmployeeToCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AddEmployeeToCaseResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<AddEmployeeToCaseResponse>>> AddEmployee(
        [FromRoute] Guid id,
        [FromBody] AddEmployeeToCaseRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { DisciplinaryCaseId = id };
        var result = await _addEmployeeToCaseUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Anexar evidência ao processo disciplinar
    /// </summary>
    /// <description>Registra uma evidência documental ou física vinculada ao processo disciplinar.</description>
    [HttpPost("{id:guid}/evidences")]
    [Permission(PermissionCodes.DisciplinaryCases.AddEvidence)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<AddEvidenceResponse>>> AddEvidence(
        [FromRoute] Guid id,
        [FromBody] AddEvidenceHttpRequest request,
        CancellationToken cancellationToken)
    {
        // Escopo por gerência: fora dele o processo "não existe".
        var visibility = HttpContext?.RequestServices?.GetService<SIGH.Application.Common.Authorization.ICaseVisibility>();
        if (visibility is not null && !await visibility.IsVisibleAsync(id, cancellationToken))
            return NotFoundResult<AddEvidenceResponse>("Processo disciplinar não encontrado.");

        var applicationRequest = request.ToApplicationRequest(id, GetUserId());
        var result = await _addEvidenceUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Registrar decisão no processo disciplinar
    /// </summary>
    /// <description>Registra a decisão fundamentada da autoridade julgadora para o processo.</description>
    [HttpPost("{id:guid}/decisions")]
    [Permission(PermissionCodes.DisciplinaryCases.RecordDecision)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<RecordDecisionResponse>>> RecordDecision(
        [FromRoute] Guid id,
        [FromBody] RecordDecisionHttpRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = request.ToApplicationRequest(id, GetUserId());
        var result = await _recordDecisionUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Aplicar medida disciplinar ao funcionário
    /// </summary>
    /// <description>Aplica a medida disciplinar acordada (ex: advertência escrita, suspensão) ao funcionário envolvido.</description>
    [HttpPost("{id:guid}/measures")]
    [Permission(PermissionCodes.DisciplinaryCases.ApplyMeasure)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ApplyMeasureResponse>>> ApplyMeasure(
        [FromRoute] Guid id,
        [FromBody] ApplyMeasureHttpRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = request.ToApplicationRequest(id, GetUserId());
        var result = await _applyMeasureUseCase.ExecuteAsync(applicationRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Cancelar processo disciplinar
    /// </summary>
    /// <description>Cancela o processo disciplinar informando o motivo do cancelamento.</description>
    [HttpPost("{id:guid}/cancel")]
    [Permission(PermissionCodes.DisciplinaryCases.Cancel)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<CancelDisciplinaryCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CancelDisciplinaryCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CancelDisciplinaryCaseResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<CancelDisciplinaryCaseResponse>>> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelDisciplinaryCaseRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { DisciplinaryCaseId = id };
        var result = await _cancelDisciplinaryCaseUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Concluir processo disciplinar
    /// </summary>
    /// <description>Finaliza e conclui formalmente o processo disciplinar com um resumo conclusivo.</description>
    [HttpPost("{id:guid}/conclude")]
    [Permission(PermissionCodes.DisciplinaryCases.Conclude)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ConcludeDisciplinaryCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ConcludeDisciplinaryCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ConcludeDisciplinaryCaseResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ConcludeDisciplinaryCaseResponse>>> Conclude(
        [FromRoute] Guid id,
        [FromBody] ConcludeDisciplinaryCaseRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { DisciplinaryCaseId = id };
        var result = await _concludeDisciplinaryCaseUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<Result<T>> ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (IsNotFoundResult(result.ErrorCode, result.Message))
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }

    private ActionResult<Result<T>> ToCreatedActionResult<T>(string actionName, object? routeValues, Result<T> result)
    {
        if (result.Success)
        {
            return CreatedAtAction(actionName, routeValues, result);
        }

        if (IsNotFoundResult(result.ErrorCode, result.Message))
        {
            return NotFound(result);
        }

        if (IsConflictErrorCode(result.ErrorCode))
        {
            return ConflictResult(result);
        }

        return BadRequest(result);
    }

    private static bool IsNotFoundResult(string? errorCode, string? message)
    {
        if (!string.IsNullOrEmpty(errorCode) &&
            (errorCode.Equals("NotFound", StringComparison.OrdinalIgnoreCase) ||
             errorCode.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(message) &&
            (message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
             message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Linha do tempo de auditoria do processo e das suas subentidades (exige Audit.View).
    /// </summary>
    [HttpGet("{id:guid}/audit-trail")]
    [Permission(PermissionCodes.Audit.View)]
    [ProducesResponseType(typeof(Result<PagedResult<SIGH.Application.Audit.AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PagedResult<SIGH.Application.Audit.AuditLogDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<PagedResult<SIGH.Application.Audit.AuditLogDto>>>> GetAuditTrail(
        [FromRoute] Guid id,
        [FromServices] SIGH.Application.Audit.IAuditLogQueryService auditQuery,
        [FromServices] SIGH.Application.Common.Authorization.ICaseVisibility visibility,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await visibility.IsVisibleAsync(id, cancellationToken))
            return NotFoundResult<PagedResult<SIGH.Application.Audit.AuditLogDto>>("Processo disciplinar não encontrado.");

        var trail = await auditQuery.GetDisciplinaryCaseTrailAsync(id, pageNumber, pageSize, cancellationToken);
        return trail is null
            ? NotFoundResult<PagedResult<SIGH.Application.Audit.AuditLogDto>>("Processo disciplinar não encontrado.")
            : OkResult(trail);
    }
}
