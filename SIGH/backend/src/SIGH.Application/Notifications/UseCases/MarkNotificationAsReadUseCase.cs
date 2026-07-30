namespace SIGH.Application.Notifications.UseCases;

using FluentValidation;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;

public class MarkNotificationAsReadCommand
{
    public Guid NotificationId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
}

public interface IMarkNotificationAsReadUseCase
{
    Task<Result<NotificationDto>> ExecuteAsync(MarkNotificationAsReadCommand command, CancellationToken cancellationToken = default);
}

public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("NotificationId é obrigatório.");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("CompanyId é obrigatório.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");
    }
}

public class MarkNotificationAsReadUseCase : IMarkNotificationAsReadUseCase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<MarkNotificationAsReadCommand> _validator;

    public MarkNotificationAsReadUseCase(
        INotificationService notificationService,
        IValidator<MarkNotificationAsReadCommand> validator)
    {
        _notificationService = notificationService;
        _validator = validator;
    }

    public async Task<Result<NotificationDto>> ExecuteAsync(MarkNotificationAsReadCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result<NotificationDto>.Failure("Dados inválidos para marcar notificação como lida.", "VALIDATION_ERROR", errors);
        }

        return await _notificationService.MarkAsReadAsync(command.NotificationId, command.CompanyId, command.UserId, cancellationToken);
    }
}
