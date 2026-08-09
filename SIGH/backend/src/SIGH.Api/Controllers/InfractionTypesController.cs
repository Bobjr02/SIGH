using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;
using SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/v1/infraction-types")]
[Tags("InfractionTypes")]
[Authorize]
public class InfractionTypesController : BaseController
{
    private readonly ICreateInfractionTypeUseCase _createInfractionTypeUseCase;
    private readonly IGetInfractionTypeByIdUseCase _getInfractionTypeByIdUseCase;
    private readonly IGetInfractionTypesUseCase _getInfractionTypesUseCase;
    private readonly IUpdateInfractionTypeUseCase _updateInfractionTypeUseCase;
    private readonly IActivateInfractionTypeUseCase _activateInfractionTypeUseCase;
    private readonly IDeactivateInfractionTypeUseCase _deactivateInfractionTypeUseCase;

    public InfractionTypesController(
        ICreateInfractionTypeUseCase createInfractionTypeUseCase,
        IGetInfractionTypeByIdUseCase getInfractionTypeByIdUseCase,
        IGetInfractionTypesUseCase getInfractionTypesUseCase,
        IUpdateInfractionTypeUseCase updateInfractionTypeUseCase,
        IActivateInfractionTypeUseCase activateInfractionTypeUseCase,
        IDeactivateInfractionTypeUseCase deactivateInfractionTypeUseCase)
    {
        _createInfractionTypeUseCase = createInfractionTypeUseCase;
        _getInfractionTypeByIdUseCase = getInfractionTypeByIdUseCase;
        _getInfractionTypesUseCase = getInfractionTypesUseCase;
        _updateInfractionTypeUseCase = updateInfractionTypeUseCase;
        _activateInfractionTypeUseCase = activateInfractionTypeUseCase;
        _deactivateInfractionTypeUseCase = deactivateInfractionTypeUseCase;
    }

    /// <summary>
    /// Cadastrar um novo tipo de infração disciplinar
    /// </summary>
    /// <description>Cadastra um novo tipo de infração disciplinar no sistema SIGH.</description>
    [HttpPost]
    [Permission("Disciplinary.InfractionTypes.Create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<CreateInfractionTypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CreateInfractionTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<CreateInfractionTypeResponse>>> Create(
        [FromBody] CreateInfractionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createInfractionTypeUseCase.ExecuteAsync(request, cancellationToken);
        return ToCreatedActionResult(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Obter tipo de infração por ID
    /// </summary>
    /// <description>Retorna os detalhes de um tipo de infração disciplinar por seu identificador único.</description>
    [HttpGet("/api/v1/InfractionTypes/{id:guid}", Name = "GetInfractionTypeById")]
    [HttpGet("/api/v1/infraction-types/{id:guid}")]
    [Permission("Disciplinary.InfractionTypes.View")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<GetInfractionTypeByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GetInfractionTypeByIdResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<GetInfractionTypeByIdResponse>>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getInfractionTypeByIdUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Listar tipos de infração com paginação e filtros
    /// </summary>
    /// <description>Obtém a listagem paginada de tipos de infração disciplinar cadastrados.</description>
    [HttpGet]
    [Permission("Disciplinary.InfractionTypes.View")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<PagedResult<InfractionTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<PagedResult<InfractionTypeDto>>>> GetPaged(
        [FromQuery] GetInfractionTypesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getInfractionTypesUseCase.ExecuteAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Atualizar dados do tipo de infração
    /// </summary>
    /// <description>Atualiza os dados cadastrais de um tipo de infração disciplinar existente.</description>
    [HttpPut("{id:guid}")]
    [Permission("Disciplinary.InfractionTypes.Update")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<UpdateInfractionTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UpdateInfractionTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UpdateInfractionTypeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<UpdateInfractionTypeResponse>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateInfractionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { Id = id };
        var result = await _updateInfractionTypeUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Ativar tipo de infração
    /// </summary>
    /// <description>Ativa um tipo de infração disciplinar inativo.</description>
    [HttpPost("{id:guid}/activate")]
    [Permission("Disciplinary.InfractionTypes.Activate")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<ActivateInfractionTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ActivateInfractionTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ActivateInfractionTypeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<ActivateInfractionTypeResponse>>> Activate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var request = new ActivateInfractionTypeRequest(id);
        var result = await _activateInfractionTypeUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Desativar tipo de infração
    /// </summary>
    /// <description>Desativa um tipo de infração disciplinar ativo.</description>
    [HttpPost("{id:guid}/deactivate")]
    [Permission("Disciplinary.InfractionTypes.Deactivate")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<DeactivateInfractionTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DeactivateInfractionTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DeactivateInfractionTypeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<DeactivateInfractionTypeResponse>>> Deactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeactivateInfractionTypeRequest(id);
        var result = await _deactivateInfractionTypeUseCase.ExecuteAsync(request, cancellationToken);
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
