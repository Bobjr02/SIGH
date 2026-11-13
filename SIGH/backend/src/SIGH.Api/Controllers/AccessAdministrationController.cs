using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Users.Access;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Administração de acesso: perfis (com validade) e gerências de cada usuário, e os nomes das gerências.
/// O ator vem sempre do token; ninguém altera o próprio acesso (403).
/// </summary>
[Route("api/v1")]
[Tags("Access administration")]
[Authorize]
public class AccessAdministrationController : BaseController
{
    private readonly IUserAccessAdministrationService _access;
    private readonly IManagementUnitAdministrationService _units;

    public AccessAdministrationController(IUserAccessAdministrationService access, IManagementUnitAdministrationService units)
    {
        _access = access;
        _units = units;
    }

    /// <summary>Perfis de sistema com as permissões fixas de cada um.</summary>
    [HttpGet("roles")]
    [Permission(PermissionCodes.Users.View)]
    public async Task<ActionResult<Result<IReadOnlyList<RoleOptionDto>>>> GetRoles(CancellationToken cancellationToken) =>
        OkResult(await _access.GetRolesAsync(cancellationToken));

    [HttpGet("users/{id:guid}/access")]
    [Permission(PermissionCodes.Users.View)]
    public async Task<ActionResult<Result<UserAccessDto>>> GetUserAccess([FromRoute] Guid id, CancellationToken cancellationToken) =>
        OkResult(await _access.GetUserAccessAsync(id, cancellationToken));

    [HttpPost("users/{id:guid}/roles")]
    [Permission(PermissionCodes.Users.ManageRoles)]
    public async Task<ActionResult<Result<UserAccessDto>>> AssignRole([FromRoute] Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken) =>
        OkResult(await _access.AssignRoleAsync(GetUserId(), id, request, cancellationToken), "Perfil atribuído.");

    [HttpDelete("users/{id:guid}/roles/{userRoleId:guid}")]
    [Permission(PermissionCodes.Users.ManageRoles)]
    public async Task<ActionResult<Result<UserAccessDto>>> RevokeRole([FromRoute] Guid id, [FromRoute] Guid userRoleId, CancellationToken cancellationToken) =>
        OkResult(await _access.RevokeRoleAsync(GetUserId(), id, userRoleId, cancellationToken), "Perfil retirado.");

    [HttpPost("users/{id:guid}/management-units")]
    [Permission(PermissionCodes.Users.ManageRoles)]
    public async Task<ActionResult<Result<UserAccessDto>>> AssignManagementUnit([FromRoute] Guid id, [FromBody] AssignManagementUnitRequest request, CancellationToken cancellationToken) =>
        OkResult(await _access.AssignManagementUnitAsync(GetUserId(), id, request, cancellationToken), "Gerência vinculada.");

    [HttpDelete("users/{id:guid}/management-units/{linkId:guid}")]
    [Permission(PermissionCodes.Users.ManageRoles)]
    public async Task<ActionResult<Result<UserAccessDto>>> RevokeManagementUnit([FromRoute] Guid id, [FromRoute] Guid linkId, CancellationToken cancellationToken) =>
        OkResult(await _access.RevokeManagementUnitAsync(GetUserId(), id, linkId, cancellationToken), "Gerência retirada.");

    /// <summary>As gerências da empresa (cadastro, não dado de funcionário: fora do escopo).</summary>
    [HttpGet("management-units")]
    [Permission(PermissionCodes.Users.View)]
    public async Task<ActionResult<Result<IReadOnlyList<ManagementUnitDto>>>> GetManagementUnits(CancellationToken cancellationToken) =>
        OkResult(await _units.GetAllAsync(cancellationToken));

    [HttpPut("management-units/{id:guid}")]
    [Permission(PermissionCodes.ManagementUnits.Manage)]
    public async Task<ActionResult<Result<ManagementUnitDto>>> RenameManagementUnit([FromRoute] Guid id, [FromBody] RenameManagementUnitRequest request, CancellationToken cancellationToken) =>
        OkResult(await _units.RenameAsync(id, request, cancellationToken), "Gerência atualizada.");
}
