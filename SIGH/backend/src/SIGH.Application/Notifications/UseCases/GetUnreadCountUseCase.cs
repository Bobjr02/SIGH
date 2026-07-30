namespace SIGH.Application.Notifications.UseCases;

using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;

public class GetUnreadCountQuery
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
}

public interface IGetUnreadCountUseCase
{
    Task<Result<int>> ExecuteAsync(GetUnreadCountQuery query, CancellationToken cancellationToken = default);
}

public class GetUnreadCountUseCase : IGetUnreadCountUseCase
{
    private readonly INotificationService _notificationService;

    public GetUnreadCountUseCase(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result<int>> ExecuteAsync(GetUnreadCountQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null || query.CompanyId == Guid.Empty || query.UserId == Guid.Empty)
        {
            return Result<int>.Failure("CompanyId e UserId são obrigatórios.", "REQUIRED_FIELDS_MISSING");
        }

        return await _notificationService.GetUnreadCountAsync(query.CompanyId, query.UserId, cancellationToken);
    }
}
