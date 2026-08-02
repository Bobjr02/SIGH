namespace SIGH.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Models;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Options;
using SIGH.Domain.Notifications.Entities;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationOptions _options;
    private readonly INotificationRepository? _repository;
    private readonly IApplicationDbContext? _dbContext;
    private readonly IDateTimeProvider? _dateTimeProvider;

    public NotificationService(
        ILogger<NotificationService> logger,
        IOptions<NotificationOptions> options,
        INotificationRepository? repository = null,
        IApplicationDbContext? dbContext = null,
        IDateTimeProvider? dateTimeProvider = null)
    {
        _logger = logger;
        _options = options.Value;
        _repository = repository;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<NotificationDto>> CreateNotificationAsync(CreateNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return Result<NotificationDto>.Failure("A requisição de notificação não pode ser nula.", "INVALID_REQUEST");

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            return Result<NotificationDto>.Failure("Título e mensagem são obrigatórios.", "REQUIRED_FIELDS_MISSING");

        if (request.CompanyId == Guid.Empty || request.UserId == Guid.Empty)
            return Result<NotificationDto>.Failure("CompanyId e UserId são obrigatórios.", "REQUIRED_FIELDS_MISSING");

        var notification = Notification.Create(
            request.CompanyId,
            request.UserId,
            request.Title,
            request.Message,
            request.Type,
            request.Priority,
            request.DueDate,
            request.Metadata);

        if (_repository != null && _dbContext != null)
        {
            await _repository.AddAsync(notification, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "[NotificationService] Notificação criada com sucesso. Id: {Id}, CompanyId: {CompanyId}, UserId: {UserId}, Título: '{Title}'.",
            notification.Id, notification.CompanyId, notification.UserId, notification.Title);

        var dto = MapToDto(notification);
        return Result<NotificationDto>.Ok(dto, "Notificação criada com sucesso.");
    }

    public async Task<Result<NotificationDto>> MarkAsReadAsync(Guid notificationId, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
            return Result<NotificationDto>.Failure("Id da notificação inválido.", "INVALID_NOTIFICATION_ID");

        if (_repository == null || _dbContext == null)
        {
            return Result<NotificationDto>.Failure("Repositório de notificações não disponível.", "REPOSITORY_UNAVAILABLE");
        }

        var notification = await _repository.GetByIdForUserAsync(notificationId, companyId, userId, cancellationToken);
        if (notification == null)
        {
            return Result<NotificationDto>.Failure("Notificação não encontrada ou não pertence ao usuário informado.", "NOTIFICATION_NOT_FOUND");
        }

        var now = _dateTimeProvider?.UtcNow ?? DateTimeOffset.UtcNow;
        notification.MarkAsRead(now);

        await _repository.UpdateAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[NotificationService] Notificação {Id} marcada como lida pelo usuário {UserId}.", notificationId, userId);

        return Result<NotificationDto>.Ok(MapToDto(notification), "Notificação marcada como lida.");
    }

    public async Task<Result<int>> MarkAllAsReadAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty || userId == Guid.Empty)
            return Result<int>.Failure("CompanyId e UserId são obrigatórios.", "REQUIRED_FIELDS_MISSING");

        if (_repository == null || _dbContext == null)
        {
            return Result<int>.Failure("Repositório de notificações não disponível.", "REPOSITORY_UNAVAILABLE");
        }

        var now = _dateTimeProvider?.UtcNow ?? DateTimeOffset.UtcNow;
        var count = await _repository.MarkAllAsReadAsync(companyId, userId, now, cancellationToken);
        if (count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("[NotificationService] {Count} notificações marcadas como lidas para o usuário {UserId}.", count, userId);

        return Result<int>.Ok(count, $"{count} notificações marcadas como lidas.");
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(
        Guid companyId,
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = new SIGH.Application.Notifications.Queries.GetNotificationsQuery
        {
            CompanyId = companyId,
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return await GetUserNotificationsAsync(q, cancellationToken);
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(
        SIGH.Application.Notifications.Queries.GetNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (_repository == null)
        {
            var empty = new PagedResult<NotificationDto>(new List<NotificationDto>(), query.PageNumber, query.PageSize, 0);
            return Result<PagedResult<NotificationDto>>.Ok(empty);
        }

        var result = await _repository.GetPagedUserNotificationsAsync(query, cancellationToken);
        return Result<PagedResult<NotificationDto>>.Ok(result);
    }

    public async Task<Result<IEnumerable<NotificationDto>>> GetUnreadNotificationsAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_repository == null)
        {
            return Result<IEnumerable<NotificationDto>>.Ok(Enumerable.Empty<NotificationDto>());
        }

        var result = await _repository.GetUnreadUserNotificationsAsync(companyId, userId, cancellationToken);
        return Result<IEnumerable<NotificationDto>>.Ok(result);
    }

    public async Task<Result<int>> GetUnreadCountAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_repository == null)
        {
            return Result<int>.Ok(0);
        }

        var count = await _repository.GetUnreadCountAsync(companyId, userId, cancellationToken);
        return Result<int>.Ok(count);
    }


    public async Task<Result<NotificationDto>> SendNotificationAsync(SendNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return Result<NotificationDto>.Failure("A requisição de notificação não pode ser nula.", "INVALID_REQUEST");

        var createReq = new CreateNotificationRequestDto
        {
            CompanyId = request.RecipientUserId ?? Guid.NewGuid(),
            UserId = request.RecipientUserId ?? Guid.NewGuid(),
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Priority = "Medium",
            Metadata = request.Metadata != null ? string.Join(";", request.Metadata.Select(kv => $"{kv.Key}={kv.Value}")) : null
        };

        return await CreateNotificationAsync(createReq, cancellationToken);
    }

    public async Task<Result<int>> SendBatchNotificationsAsync(IEnumerable<SendNotificationRequestDto> requests, CancellationToken cancellationToken = default)
    {
        if (requests == null)
            return Result<int>.Failure("A lista de notificações não pode ser nula.", "INVALID_REQUEST");

        int count = 0;
        foreach (var req in requests)
        {
            var res = await SendNotificationAsync(req, cancellationToken);
            if (res.Success) count++;
        }

        return Result<int>.Ok(count, $"Processadas {count} notificações.");
    }

    public async Task<Result<IEnumerable<NotificationDto>>> GetPendingNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_repository == null)
            return Result<IEnumerable<NotificationDto>>.Ok(Enumerable.Empty<NotificationDto>());

        var unread = await _repository.GetUnreadUserNotificationsAsync(Guid.Empty, userId, cancellationToken);
        return Result<IEnumerable<NotificationDto>>.Ok(unread);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            CompanyId = notification.CompanyId,
            RecipientUserId = notification.UserId,
            RecipientEmail = string.Empty,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Priority = notification.Priority,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            IsExpired = notification.IsExpired,
            DueDate = notification.DueDate,
            Metadata = notification.Metadata
        };
    }
}
