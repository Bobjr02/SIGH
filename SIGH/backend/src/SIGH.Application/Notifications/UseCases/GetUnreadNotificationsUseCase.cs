namespace SIGH.Application.Notifications.UseCases;

using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;

public class GetUnreadNotificationsQuery
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
}

public interface IGetUnreadNotificationsUseCase
{
    Task<Result<IEnumerable<NotificationDto>>> ExecuteAsync(GetUnreadNotificationsQuery query, CancellationToken cancellationToken = default);
}

public class GetUnreadNotificationsUseCase : IGetUnreadNotificationsUseCase
{
    private readonly INotificationService _notificationService;

    public GetUnreadNotificationsUseCase(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result<IEnumerable<NotificationDto>>> ExecuteAsync(GetUnreadNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null || query.CompanyId == Guid.Empty || query.UserId == Guid.Empty)
        {
            return Result<IEnumerable<NotificationDto>>.Failure("CompanyId e UserId são obrigatórios.", "REQUIRED_FIELDS_MISSING");
        }

        return await _notificationService.GetUnreadNotificationsAsync(query.CompanyId, query.UserId, cancellationToken);
    }
}
