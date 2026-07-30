namespace SIGH.Application.Interfaces.Repositories;

using SIGH.Application.Common.Models;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.Queries;
using SIGH.Domain.Notifications.Entities;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdForUserAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationDto>> GetPagedUserNotificationsAsync(Guid companyId, Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationDto>> GetPagedUserNotificationsAsync(GetNotificationsQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationDto>> GetUnreadUserNotificationsAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid companyId, Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<NotificationLog?> GetLogAsync(Guid companyId, string sourceEntity, Guid sourceEntityId, Guid recipientUserId, string eventType, CancellationToken cancellationToken = default);
    Task AddLogAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task UpdateLogAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationLog>> GetLogsByEntityAsync(Guid companyId, string sourceEntity, Guid sourceEntityId, CancellationToken cancellationToken = default);
}
