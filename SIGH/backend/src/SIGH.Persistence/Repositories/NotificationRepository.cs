namespace SIGH.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.Queries;
using SIGH.Domain.Notifications.Entities;
using SIGH.Persistence.Context;

public class NotificationRepository : INotificationRepository
{
    private readonly SighDbContext _context;
    private readonly SIGH.Application.Common.Authorization.IAccessScope? _scope;

    public NotificationRepository(SighDbContext context, SIGH.Application.Common.Authorization.IAccessScope? scope = null)
    {
        _context = context;
        _scope = scope;
    }

    /// <summary>
    /// Notificações do usuário, sem as que se referem a uma medida/processo fora do escopo atual dele
    /// (por exemplo, depois de perder a gerência). Notificações sem processo vinculado sempre aparecem.
    /// </summary>
    private async Task<IQueryable<Notification>> UserNotificationsAsync(Guid companyId, Guid userId, CancellationToken cancellationToken)
    {
        var query = _context.Notifications.AsNoTracking().Where(n => n.CompanyId == companyId && n.UserId == userId);
        if (_scope is null)
            return query;

        var filter = await _scope.GetFilterAsync(cancellationToken);
        if (filter.All)
            return query;

        var visibleCases = SIGH.Application.Common.Authorization.AccessScopeQueryExtensions.VisibleCaseIds(
            _context.DisciplinaryCaseEmployees, _context.Employees, filter);
        return query.Where(n => n.DisciplinaryCaseId == null || visibleCases.Contains(n.DisciplinaryCaseId.Value));
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId, cancellationToken);
    }

    public async Task<Notification?> GetByIdForUserAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId && n.UserId == userId, cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetPagedUserNotificationsAsync(
        Guid companyId,
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = new GetNotificationsQuery
        {
            CompanyId = companyId,
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return await GetPagedUserNotificationsAsync(q, cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetPagedUserNotificationsAsync(
        GetNotificationsQuery queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = await UserNotificationsAsync(queryParams.CompanyId, queryParams.UserId, cancellationToken);

        if (queryParams.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == queryParams.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Type))
        {
            var typeLower = queryParams.Type.Trim().ToLower();
            query = query.Where(n => n.Type.ToLower() == typeLower);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Priority))
        {
            var priorityLower = queryParams.Priority.Trim().ToLower();
            query = query.Where(n => n.Priority.ToLower() == priorityLower);
        }

        if (queryParams.StartDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= queryParams.StartDate.Value);
        }

        if (queryParams.EndDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= queryParams.EndDate.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var page = queryParams.PageNumber <= 0 ? 1 : queryParams.PageNumber;
        var size = queryParams.PageSize <= 0 ? 10 : (queryParams.PageSize > 100 ? 100 : queryParams.PageSize);

        var isAsc = string.Equals(queryParams.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var sortBy = (queryParams.SortBy ?? string.Empty).Trim().ToLower();

        query = sortBy switch
        {
            "priority" => isAsc ? query.OrderBy(n => n.Priority) : query.OrderByDescending(n => n.Priority),
            "type" => isAsc ? query.OrderBy(n => n.Type) : query.OrderByDescending(n => n.Type),
            "isread" => isAsc ? query.OrderBy(n => n.IsRead) : query.OrderByDescending(n => n.IsRead),
            "duedate" => isAsc ? query.OrderBy(n => n.DueDate) : query.OrderByDescending(n => n.DueDate),
            _ => isAsc ? query.OrderBy(n => n.CreatedAt) : query.OrderByDescending(n => n.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                CompanyId = n.CompanyId,
                RecipientUserId = n.UserId,
                RecipientEmail = string.Empty,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Priority = n.Priority,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                IsExpired = n.IsExpired,
                DueDate = n.DueDate,
                Metadata = n.Metadata
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, page, size, totalItems);
    }

    public async Task<IEnumerable<NotificationDto>> GetUnreadUserNotificationsAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (await UserNotificationsAsync(companyId, userId, cancellationToken))
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                CompanyId = n.CompanyId,
                RecipientUserId = n.UserId,
                RecipientEmail = string.Empty,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Priority = n.Priority,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                IsExpired = n.IsExpired,
                DueDate = n.DueDate,
                Metadata = n.Metadata
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (await UserNotificationsAsync(companyId, userId, cancellationToken))
            .CountAsync(n => !n.IsRead, cancellationToken);
    }

    public async Task<int> MarkAllAsReadAsync(
        Guid companyId,
        Guid userId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.CompanyId == companyId && n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (!unreadNotifications.Any())
        {
            return 0;
        }

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(readAt);
        }

        return unreadNotifications.Count;
    }


    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<NotificationLog?> GetLogAsync(
        Guid companyId,
        string sourceEntity,
        Guid sourceEntityId,
        Guid recipientUserId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .FirstOrDefaultAsync(l =>
                l.CompanyId == companyId &&
                l.SourceEntity == sourceEntity &&
                l.SourceEntityId == sourceEntityId &&
                l.RecipientUserId == recipientUserId &&
                l.EventType == eventType,
                cancellationToken);
    }

    public async Task AddLogAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        await _context.NotificationLogs.AddAsync(log, cancellationToken);
    }

    public Task UpdateLogAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        _context.NotificationLogs.Update(log);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<NotificationLog>> GetLogsByEntityAsync(
        Guid companyId,
        string sourceEntity,
        Guid sourceEntityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.SourceEntity == sourceEntity && l.SourceEntityId == sourceEntityId)
            .ToListAsync(cancellationToken);
    }
}
