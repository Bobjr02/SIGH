namespace SIGH.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.Common;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.Queries;
using SIGH.Application.Notifications.UseCases;
using SIGH.Infrastructure.Authorization;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
[Authorize]
public class NotificationsController : BaseController
{
    private readonly IGetUserNotificationsUseCase _getUserNotificationsUseCase;
    private readonly IGetUnreadNotificationsUseCase _getUnreadNotificationsUseCase;
    private readonly IGetUnreadCountUseCase _getUnreadCountUseCase;
    private readonly IMarkNotificationAsReadUseCase _markNotificationAsReadUseCase;
    private readonly IMarkAllNotificationsAsReadUseCase _markAllNotificationsAsReadUseCase;
    private readonly IProcessDeadlineMonitoringUseCase _processDeadlineMonitoringUseCase;
    private readonly IAuthorizedCompanyProvider _authorizedCompanyProvider;

    public NotificationsController(
        IGetUserNotificationsUseCase getUserNotificationsUseCase,
        IGetUnreadNotificationsUseCase getUnreadNotificationsUseCase,
        IGetUnreadCountUseCase getUnreadCountUseCase,
        IMarkNotificationAsReadUseCase markNotificationAsReadUseCase,
        IMarkAllNotificationsAsReadUseCase markAllNotificationsAsReadUseCase,
        IProcessDeadlineMonitoringUseCase processDeadlineMonitoringUseCase,
        IAuthorizedCompanyProvider authorizedCompanyProvider)
    {
        _getUserNotificationsUseCase = getUserNotificationsUseCase;
        _getUnreadNotificationsUseCase = getUnreadNotificationsUseCase;
        _getUnreadCountUseCase = getUnreadCountUseCase;
        _markNotificationAsReadUseCase = markNotificationAsReadUseCase;
        _markAllNotificationsAsReadUseCase = markAllNotificationsAsReadUseCase;
        _processDeadlineMonitoringUseCase = processDeadlineMonitoringUseCase;
        _authorizedCompanyProvider = authorizedCompanyProvider;
    }

    /// <summary>
    /// Consulta paginada das notificações do usuário autenticado.
    /// </summary>
    [HttpGet]
    [RequirePermission(NotificationPermissions.View)]
    [ProducesResponseType(typeof(Result<PagedResult<NotificationDto>>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<PagedResult<NotificationDto>>>> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isRead = null,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);

        var query = new GetNotificationsQuery
        {
            CompanyId = resolvedCompanyId,
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            IsRead = isRead,
            Type = type,
            Priority = priority,
            StartDate = startDate,
            EndDate = endDate,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _getUserNotificationsUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            return BadRequestResult<PagedResult<NotificationDto>>(result.Message ?? "Erro ao consultar notificações.", result.ErrorCode, result.ValidationErrors);
        }

        return OkResult(result.Data!);
    }

    /// <summary>
    /// Retorna todas as notificações não lidas do usuário autenticado.
    /// </summary>
    [HttpGet("unread")]
    [RequirePermission(NotificationPermissions.View)]
    [ProducesResponseType(typeof(Result<IEnumerable<NotificationDto>>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<IEnumerable<NotificationDto>>>> GetUnreadNotifications(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);

        var query = new GetUnreadNotificationsQuery
        {
            CompanyId = resolvedCompanyId,
            UserId = userId
        };

        var result = await _getUnreadNotificationsUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            return BadRequestResult<IEnumerable<NotificationDto>>(result.Message ?? "Erro ao consultar notificações não lidas.", result.ErrorCode);
        }

        return OkResult(result.Data!);
    }

    /// <summary>
    /// Retorna a contagem de notificações não lidas do usuário autenticado.
    /// </summary>
    [HttpGet("unread/count")]
    [RequirePermission(NotificationPermissions.View)]
    [ProducesResponseType(typeof(Result<int>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<int>>> GetUnreadCount(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);

        var query = new GetUnreadCountQuery
        {
            CompanyId = resolvedCompanyId,
            UserId = userId
        };

        var result = await _getUnreadCountUseCase.ExecuteAsync(query, cancellationToken);
        if (!result.Success)
        {
            return BadRequestResult<int>(result.Message ?? "Erro ao consultar contagem de não lidas.", result.ErrorCode);
        }

        return OkResult(result.Data);
    }

    /// <summary>
    /// Marca uma notificação específica do usuário autenticado como lida.
    /// </summary>
    [HttpPut("{id:guid}/read")]
    [RequirePermission(NotificationPermissions.Read)]
    [ProducesResponseType(typeof(Result<NotificationDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<Result<NotificationDto>>> MarkAsRead(
        [FromRoute] Guid id,
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);

        var command = new MarkNotificationAsReadCommand
        {
            NotificationId = id,
            CompanyId = resolvedCompanyId,
            UserId = userId
        };

        var result = await _markNotificationAsReadUseCase.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "NOTIFICATION_NOT_FOUND")
            {
                return NotFoundResult<NotificationDto>(result.Message ?? "Notificação não encontrada.", result.ErrorCode);
            }
            return BadRequestResult<NotificationDto>(result.Message ?? "Erro ao marcar notificação como lida.", result.ErrorCode, result.ValidationErrors);
        }

        return OkResult(result.Data!);
    }

    /// <summary>
    /// Marca todas as notificações do usuário autenticado como lidas.
    /// </summary>
    [HttpPut("read-all")]
    [RequirePermission(NotificationPermissions.Read)]
    [ProducesResponseType(typeof(Result<int>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<int>>> MarkAllAsRead(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);

        var command = new MarkAllNotificationsAsReadCommand
        {
            CompanyId = resolvedCompanyId,
            UserId = userId
        };

        var result = await _markAllNotificationsAsReadUseCase.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            return BadRequestResult<int>(result.Message ?? "Erro ao marcar notificações como lidas.", result.ErrorCode);
        }

        return OkResult(result.Data);
    }

    /// <summary>
    /// Executa manualmente o processamento de lembretes recorrentes e escalonamento de prazos.
    /// </summary>
    [HttpPost("process-deadlines")]
    [RequirePermission(NotificationPermissions.Manage)]
    [ProducesResponseType(typeof(Result<DeadlineProcessingResultDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<ActionResult<Result<DeadlineProcessingResultDto>>> ProcessDeadlines(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedCompanyId = await _authorizedCompanyProvider.GetAuthorizedCompanyIdAsync(companyId, cancellationToken);
        var result = await _processDeadlineMonitoringUseCase.ExecuteAsync(resolvedCompanyId, cancellationToken);

        if (!result.Success)
        {
            return BadRequestResult<DeadlineProcessingResultDto>(result.Message ?? "Erro ao processar prazos.", result.ErrorCode);
        }

        return OkResult(result.Data!);
    }
}
