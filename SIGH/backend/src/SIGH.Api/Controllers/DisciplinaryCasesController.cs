using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Permission("Disciplinary.Cases.Create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<CreateDisciplinaryCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CreateDisciplinaryCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<CreateDisciplinaryCaseResponse>>> Create(
        [FromBody] CreateDisciplinaryCaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createDisciplinaryCaseUseCase.ExecuteAsync(request, cancellationToken);
        return ToCreatedActionResult(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Obter processo disciplinar por ID
    /// </summary>
    /// <description>Obtém os detalhes completos do processo disciplinar, incluindo ocorrências, envolvidos, evidências, decisões e medidas.</description>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Permission("Disciplinary.Cases.View")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<GetDisciplinaryCaseByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GetDisciplinaryCaseByIdResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<GetDisciplinaryCaseByIdResponse>>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getDisciplinaryCaseByIdUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Listar processos disciplinares com paginação e filtros
    /// </summary>
    /// <description>Retorna uma lista paginada de processos disciplinares filtrados por empresa, status, prioridade ou termo de busca.</description>
    [HttpGet]
    [Permission("Disciplinary.Cases.View")]
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
    [Permission("Disciplinary.Cases.Open")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OpenCaseResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<OpenCaseResponse>>> Open(
        [FromRoute] Guid id,
        [FromBody] OpenCaseRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = request?.OpenedByUserId != null && request.OpenedByUserId != Guid.Empty
            ? request.OpenedByUserId
            : GetUserId();
        var updatedRequest = new OpenCaseRequest(id, userId);
        var result = await _openCaseUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Iniciar investigação do processo disciplinar
    /// </summary>
    /// <description>Inicia a fase de investigação de um processo disciplinar aberto.</description>
    [HttpPost("{id:guid}/start-investigation")]
    [Permission("Disciplinary.Cases.StartInvestigation")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<StartInvestigationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<StartInvestigationResponse>>> StartInvestigation(
        [FromRoute] Guid id,
        [FromBody] StartInvestigationRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = request?.InvestigatorUserId != null && request.InvestigatorUserId != Guid.Empty
            ? request.InvestigatorUserId
            : GetUserId();
        var updatedRequest = new StartInvestigationRequest(id, userId);
        var result = await _startInvestigationUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Submeter processo disciplinar para decisão
    /// </summary>
    /// <description>Encaminha o processo sob investigação para a etapa de julgamento/decisão.</description>
    [HttpPost("{id:guid}/submit-for-decision")]
    [Permission("Disciplinary.Cases.SubmitForDecision")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SubmitCaseForDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<SubmitCaseForDecisionResponse>>> SubmitForDecision(
        [FromRoute] Guid id,
        [FromBody] SubmitCaseForDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = request?.SubmittedByUserId != null && request.SubmittedByUserId != Guid.Empty
            ? request.SubmittedByUserId
            : GetUserId();
        var updatedRequest = new SubmitCaseForDecisionRequest(id, userId);
        var result = await _submitCaseForDecisionUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Aprovar decisão proferida no processo disciplinar
    /// </summary>
    /// <description>Aprova a decisão registrada para o processo disciplinar.</description>
    [HttpPost("{id:guid}/approve-decision")]
    [Permission("Disciplinary.Cases.ApproveDecision")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ApproveDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ApproveDecisionResponse>>> ApproveDecision(
        [FromRoute] Guid id,
        [FromBody] ApproveDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = request?.ApprovedByUserId != null && request.ApprovedByUserId != Guid.Empty
            ? request.ApprovedByUserId
            : GetUserId();
        var updatedRequest = new ApproveDecisionRequest(id, userId);
        var result = await _approveDecisionUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Rejeitar decisão proferida no processo disciplinar
    /// </summary>
    /// <description>Rejeita a decisão registrada com justificativa e retorna para análise.</description>
    [HttpPost("{id:guid}/reject-decision")]
    [Permission("Disciplinary.Cases.RejectDecision")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<RejectDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<RejectDecisionResponse>>> RejectDecision(
        [FromRoute] Guid id,
        [FromBody] RejectDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = request.RejectedByUserId != Guid.Empty
            ? request.RejectedByUserId
            : GetUserId();
        var updatedRequest = request with { DisciplinaryCaseId = id, RejectedByUserId = userId };
        var result = await _rejectDecisionUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Adicionar ocorrência de infração ao processo disciplinar
    /// </summary>
    /// <description>Registra uma nova ocorrência com tipo de infração, gravidade e data no processo disciplinar.</description>
    [HttpPost("{id:guid}/occurrences")]
    [Permission("Disciplinary.Cases.AddOccurrence")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AddOccurrenceResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<AddOccurrenceResponse>>> AddOccurrence(
        [FromRoute] Guid id,
        [FromBody] AddOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = request.ReportedByUserId != Guid.Empty ? request.ReportedByUserId : GetUserId();
        var updatedRequest = request with { DisciplinaryCaseId = id, ReportedByUserId = userId };
        var result = await _addOccurrenceUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Associar funcionário (envolvido) ao processo disciplinar
    /// </summary>
    /// <description>Adiciona um funcionário ao processo com papel específico (Acusado, Testemunha, Noticiante, etc.).</description>
    [HttpPost("{id:guid}/employees")]
    [Permission("Disciplinary.Cases.AddEmployee")]
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
    [Permission("Disciplinary.Cases.AddEvidence")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AddEvidenceResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<AddEvidenceResponse>>> AddEvidence(
        [FromRoute] Guid id,
        [FromBody] AddEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = request.CollectedByUserId != Guid.Empty ? request.CollectedByUserId : GetUserId();
        var updatedRequest = request with { DisciplinaryCaseId = id, CollectedByUserId = userId };
        var result = await _addEvidenceUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Registrar decisão no processo disciplinar
    /// </summary>
    /// <description>Registra a decisão fundamentada da autoridade julgadora para o processo.</description>
    [HttpPost("{id:guid}/decisions")]
    [Permission("Disciplinary.Cases.RecordDecision")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<RecordDecisionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<RecordDecisionResponse>>> RecordDecision(
        [FromRoute] Guid id,
        [FromBody] RecordDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = request.DecidedByUserId != Guid.Empty ? request.DecidedByUserId : GetUserId();
        var updatedRequest = request with { DisciplinaryCaseId = id, DecidedByUserId = userId };
        var result = await _recordDecisionUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Aplicar medida disciplinar ao funcionário
    /// </summary>
    /// <description>Aplica a medida disciplinar acordada (ex: advertência escrita, suspensão) ao funcionário envolvido.</description>
    [HttpPost("{id:guid}/measures")]
    [Permission("Disciplinary.Cases.ApplyMeasure")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ApplyMeasureResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ApplyMeasureResponse>>> ApplyMeasure(
        [FromRoute] Guid id,
        [FromBody] ApplyMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var userId = request.AppliedByUserId != Guid.Empty ? request.AppliedByUserId : GetUserId();
        var updatedRequest = request with { DisciplinaryCaseId = id, AppliedByUserId = userId };
        var result = await _applyMeasureUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Cancelar processo disciplinar
    /// </summary>
    /// <description>Cancela o processo disciplinar informando o motivo do cancelamento.</description>
    [HttpPost("{id:guid}/cancel")]
    [Permission("Disciplinary.Cases.Cancel")]
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
    [Permission("Disciplinary.Cases.Conclude")]
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
}
