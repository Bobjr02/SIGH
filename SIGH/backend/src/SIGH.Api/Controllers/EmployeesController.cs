using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.ChangeOrganizationalAssignment;
using SIGH.Application.Employees.ChangeStatus;
using SIGH.Application.Employees.ChangeSupervisor;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.CreateEmployee;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;
using SIGH.Application.Employees.LinkUser;
using SIGH.Application.Employees.ReactivateEmployee;
using SIGH.Application.Employees.TerminateEmployee;
using SIGH.Application.Employees.UnlinkUser;
using SIGH.Application.Employees.UpdateEmployee;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

[Route("api/v1/employees")]
[Tags("Employees")]
[Authorize]
public class EmployeesController : BaseController
{
    private readonly ICreateEmployeeUseCase _createEmployeeUseCase;
    private readonly IGetEmployeeByIdUseCase _getEmployeeByIdUseCase;
    private readonly IGetEmployeesUseCase _getEmployeesUseCase;
    private readonly IUpdateEmployeeUseCase _updateEmployeeUseCase;
    private readonly IChangeEmployeeOrganizationalAssignmentUseCase _changeEmployeeOrganizationalAssignmentUseCase;
    private readonly IChangeEmployeeSupervisorUseCase _changeEmployeeSupervisorUseCase;
    private readonly IChangeEmployeeStatusUseCase _changeEmployeeStatusUseCase;
    private readonly ITerminateEmployeeUseCase _terminateEmployeeUseCase;
    private readonly IReactivateEmployeeUseCase _reactivateEmployeeUseCase;
    private readonly ILinkEmployeeUserUseCase _linkEmployeeUserUseCase;
    private readonly IUnlinkEmployeeUserUseCase _unlinkEmployeeUserUseCase;

    public EmployeesController(
        ICreateEmployeeUseCase createEmployeeUseCase,
        IGetEmployeeByIdUseCase getEmployeeByIdUseCase,
        IGetEmployeesUseCase getEmployeesUseCase,
        IUpdateEmployeeUseCase updateEmployeeUseCase,
        IChangeEmployeeOrganizationalAssignmentUseCase changeEmployeeOrganizationalAssignmentUseCase,
        IChangeEmployeeSupervisorUseCase changeEmployeeSupervisorUseCase,
        IChangeEmployeeStatusUseCase changeEmployeeStatusUseCase,
        ITerminateEmployeeUseCase terminateEmployeeUseCase,
        IReactivateEmployeeUseCase reactivateEmployeeUseCase,
        ILinkEmployeeUserUseCase linkEmployeeUserUseCase,
        IUnlinkEmployeeUserUseCase unlinkEmployeeUserUseCase)
    {
        _createEmployeeUseCase = createEmployeeUseCase;
        _getEmployeeByIdUseCase = getEmployeeByIdUseCase;
        _getEmployeesUseCase = getEmployeesUseCase;
        _updateEmployeeUseCase = updateEmployeeUseCase;
        _changeEmployeeOrganizationalAssignmentUseCase = changeEmployeeOrganizationalAssignmentUseCase;
        _changeEmployeeSupervisorUseCase = changeEmployeeSupervisorUseCase;
        _changeEmployeeStatusUseCase = changeEmployeeStatusUseCase;
        _terminateEmployeeUseCase = terminateEmployeeUseCase;
        _reactivateEmployeeUseCase = reactivateEmployeeUseCase;
        _linkEmployeeUserUseCase = linkEmployeeUserUseCase;
        _unlinkEmployeeUserUseCase = unlinkEmployeeUserUseCase;
    }

    /// <summary>
    /// Cadastrar um novo funcionário
    /// </summary>
    /// <description>Cadastra um novo funcionário na empresa informada com validação de regras de negócio.</description>
    [HttpPost]
    [Permission("Employees.Create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<CreateEmployeeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CreateEmployeeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<CreateEmployeeResponse>>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createEmployeeUseCase.ExecuteAsync(request, cancellationToken);
        return ToCreatedActionResult(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Obter funcionário por ID
    /// </summary>
    /// <description>Retorna os detalhes completos de um funcionário a partir do seu identificador único (ID).</description>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Permission("Employees.View")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<GetEmployeeByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<GetEmployeeByIdResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<GetEmployeeByIdResponse>>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getEmployeeByIdUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Listar funcionários com paginação e filtros
    /// </summary>
    /// <description>Obtém a listagem paginada de funcionários aplicando filtros como empresa, unidade gestora, departamento, cargo, status e termo de busca.</description>
    [HttpGet]
    [Permission("Employees.View")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<PagedResult<EmployeeListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<PagedResult<EmployeeListItemResponse>>>> GetList(
        [FromQuery] GetEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getEmployeesUseCase.ExecuteAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Atualizar dados cadastrais do funcionário
    /// </summary>
    /// <description>Atualiza os dados cadastrais e de identificação de um funcionário existente.</description>
    [HttpPut("{id:guid}")]
    [Permission("Employees.Update")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<UpdateEmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UpdateEmployeeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UpdateEmployeeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<UpdateEmployeeResponse>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _updateEmployeeUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Alterar alocação organizacional do funcionário
    /// </summary>
    /// <description>Altera o cargo, unidade gestora e departamento associados ao funcionário.</description>
    [HttpPatch("{id:guid}/organizational-assignment")]
    [Permission("Employees.ChangeOrganizationalAssignment")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> ChangeOrganizationalAssignment(
        [FromRoute] Guid id,
        [FromBody] ChangeEmployeeOrganizationalAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _changeEmployeeOrganizationalAssignmentUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Alterar supervisor do funcionário
    /// </summary>
    /// <description>Define ou altera o gestor/supervisor responsável pelo funcionário.</description>
    [HttpPatch("{id:guid}/supervisor")]
    [Permission("Employees.ChangeSupervisor")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> ChangeSupervisor(
        [FromRoute] Guid id,
        [FromBody] ChangeEmployeeSupervisorRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _changeEmployeeSupervisorUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Alterar status do funcionário
    /// </summary>
    /// <description>Altera o status do funcionário conforme as regras de transição permitidas.</description>
    [HttpPatch("{id:guid}/status")]
    [Permission("Employees.ChangeStatus")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> ChangeStatus(
        [FromRoute] Guid id,
        [FromBody] ChangeEmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _changeEmployeeStatusUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Desligar funcionário
    /// </summary>
    /// <description>Efetua o desligamento/demissão do funcionário com registro de data e motivo.</description>
    [HttpPatch("{id:guid}/terminate")]
    [Permission("Employees.Terminate")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<TerminateEmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TerminateEmployeeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<TerminateEmployeeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<TerminateEmployeeResponse>>> Terminate(
        [FromRoute] Guid id,
        [FromBody] TerminateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _terminateEmployeeUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Readmitir / Reativar funcionário
    /// </summary>
    /// <description>Reativa o cadastro de um funcionário desligado informando a nova data de admissão.</description>
    [HttpPatch("{id:guid}/reactivate")]
    [Permission("Employees.Reactivate")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> Reactivate(
        [FromRoute] Guid id,
        [FromBody] ReactivateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _reactivateEmployeeUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Vincular usuário de sistema ao funcionário
    /// </summary>
    /// <description>Associa uma conta de usuário do sistema ao cadastro do funcionário.</description>
    [HttpPatch("{id:guid}/link-user")]
    [Permission("Employees.LinkUser")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> LinkUser(
        [FromRoute] Guid id,
        [FromBody] LinkEmployeeUserRequest request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = request with { EmployeeId = id };
        var result = await _linkEmployeeUserUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Desvincular usuário de sistema do funcionário
    /// </summary>
    /// <description>Remove o vínculo da conta de usuário de sistema com o cadastro do funcionário.</description>
    [HttpPatch("{id:guid}/unlink-user")]
    [Permission("Employees.UnlinkUser")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result>> UnlinkUser(
        [FromRoute] Guid id,
        [FromBody] UnlinkEmployeeUserRequest? request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = (request ?? new UnlinkEmployeeUserRequest(id)) with { EmployeeId = id };
        var result = await _unlinkEmployeeUserUseCase.ExecuteAsync(updatedRequest, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<Result<T>> ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (IsNotFoundResult(result.ErrorCode))
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }

    private ActionResult<Result> ToActionResult(Result result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (IsNotFoundResult(result.ErrorCode))
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

        if (IsNotFoundResult(result.ErrorCode))
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }

    private static bool IsNotFoundResult(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode)) return false;

        return errorCode == EmployeeErrors.NotFound ||
               errorCode == EmployeeErrors.CompanyNotFound ||
               errorCode == EmployeeErrors.JobTitleNotFound ||
               errorCode == EmployeeErrors.ManagementUnitNotFound ||
               errorCode == EmployeeErrors.DepartmentNotFound ||
               errorCode == EmployeeErrors.SupervisorNotFound ||
               errorCode == EmployeeErrors.UserNotFound ||
               errorCode.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase) ||
               errorCode.Equals("NotFound", StringComparison.OrdinalIgnoreCase);
    }
}
