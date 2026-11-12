using Microsoft.Extensions.Options;
using SIGH.Application.Options;
using SIGH.Domain.Notifications.Entities;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Email;

/// <summary>E-mail de alerta de prazo para cada notificação gerada pelo worker de prazos.</summary>
public interface IDeadlineAlertEmailer
{
    /// <summary>Enfileira o alerta (se Email:SendDeadlineAlerts=true). Entra no SaveChanges de quem chamou.</summary>
    Task EnqueueAsync(Notification notification, CancellationToken cancellationToken = default);
}

public sealed class DeadlineAlertEmailer : IDeadlineAlertEmailer
{
    private readonly IEmailOutbox _outbox;
    private readonly IUserRepository _users;
    private readonly EmailOptions _emailOptions;
    private readonly AppOptions _appOptions;

    public DeadlineAlertEmailer(IEmailOutbox outbox, IUserRepository users, IOptions<EmailOptions> emailOptions, IOptions<AppOptions> appOptions)
    {
        _outbox = outbox;
        _users = users;
        _emailOptions = emailOptions.Value;
        _appOptions = appOptions.Value;
    }

    public async Task EnqueueAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (!_emailOptions.SendDeadlineAlerts)
            return;

        var user = await _users.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return;

        _outbox.Enqueue(user.Email, EmailTemplates.DeadlineAlert(
            user.FullName, notification.Title, notification.Message, notification.DueDate, _appOptions.BuildUrl("/notifications")));
    }
}
