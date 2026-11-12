using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Audit;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Infrastructure.Authorization;

namespace SIGH.Api.Controllers;

/// <summary>
/// Consulta da trilha de auditoria. Somente leitura: a auditoria só recebe inserções e não há
/// endpoint de alteração nem de exclusão (AuditLogInsertOnlyTests garante isso).
/// </summary>
[Route("api/v1/audit-logs")]
[Tags("Audit")]
public class AuditLogsController : BaseController
{
    private readonly IAuditLogQueryService _queryService;

    public AuditLogsController(IAuditLogQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>Lista paginada (mais recentes primeiro), com filtros por entidade, id, usuário, ação e período.</summary>
    [HttpGet]
    [Permission(PermissionCodes.Audit.View)]
    [ProducesResponseType(typeof(Result<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<PagedResult<AuditLogDto>>>> Search(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.SearchAsync(
            new AuditLogFilter(entityName, entityId, userId, action, from, to, pageNumber, pageSize), cancellationToken);
        return OkResult(result);
    }
}
