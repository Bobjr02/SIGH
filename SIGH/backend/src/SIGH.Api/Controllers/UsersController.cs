using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Users.ChangeStatus;
using SIGH.Application.Users.CreateUser;
using SIGH.Application.Users.GetUserById;
using SIGH.Application.Users.GetUsers;
using SIGH.Application.Users.UnlockUser;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

[Route("api/v1/users")]
[Tags("Users")]
[Authorize]
public class UsersController : BaseController
{
    private readonly ICreateUserService _createUserService;
    private readonly IChangeUserStatusService _changeUserStatusService;
    private readonly IUnlockUserService _unlockUserService;
    private readonly IGetUsersService _getUsersService;
    private readonly IGetUserByIdService _getUserByIdService;

    public UsersController(
        ICreateUserService createUserService,
        IChangeUserStatusService changeUserStatusService,
        IUnlockUserService unlockUserService,
        IGetUsersService getUsersService,
        IGetUserByIdService getUserByIdService)
    {
        _createUserService = createUserService;
        _changeUserStatusService = changeUserStatusService;
        _unlockUserService = unlockUserService;
        _getUsersService = getUsersService;
        _getUserByIdService = getUserByIdService;
    }

    [HttpPost]
    [Permission(PermissionCodes.Users.Create)]
    [ProducesResponseType(typeof(Result<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<CreateUserResponse>>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _createUserService.CreateUserAsync(request, cancellationToken);
        return CreatedResult(nameof(GetUserById), new { id = result.Id }, result, "Usuário criado com sucesso.");
    }

    [HttpPatch("{id:guid}/status")]
    [Permission(PermissionCodes.Users.ChangeStatus)]
    [ProducesResponseType(typeof(Result<ChangeUserStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<ChangeUserStatusResponse>>> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        var updatedRequest = request with { UserId = id };
        var result = await _changeUserStatusService.ChangeUserStatusAsync(updatedRequest, cancellationToken);
        return OkResult(result, "Status do usuário alterado com sucesso.");
    }

    [HttpPost("{id:guid}/unlock")]
    [Permission(PermissionCodes.Users.Unlock)]
    [ProducesResponseType(typeof(Result<UnlockUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<UnlockUserResponse>>> UnlockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        // O usuário alvo vem da rota e o ator vem exclusivamente do token.
        var updatedRequest = new UnlockUserRequest(id, GetUserId());
        var result = await _unlockUserService.UnlockUserAsync(updatedRequest, cancellationToken);
        return OkResult(result, "Usuário desbloqueado com sucesso.");
    }

    [HttpGet]
    [Permission(PermissionCodes.Users.View)]
    [ProducesResponseType(typeof(Result<PagedList<UserSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<PagedList<UserSummaryDto>>>> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await _getUsersService.GetUsersAsync(query, cancellationToken);
        return OkResult(result, "Listagem de usuários obtida com sucesso.");
    }

    [HttpGet("{id:guid}", Name = nameof(GetUserById))]
    [Permission(PermissionCodes.Users.View)]
    [ProducesResponseType(typeof(Result<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<UserDetailDto>>> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUserByIdService.GetUserByIdAsync(id, cancellationToken);
        return OkResult(result, "Dados do usuário obtidos com sucesso.");
    }
}
