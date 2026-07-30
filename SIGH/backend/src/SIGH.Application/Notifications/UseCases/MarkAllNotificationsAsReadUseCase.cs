namespace SIGH.Application.Notifications.UseCases;

using FluentValidation;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;

public class MarkAllNotificationsAsReadCommand
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
}

public interface IMarkAllNotificationsAsReadUseCase
{
    Task<Result<int>> ExecuteAsync(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken = default);
}

public class MarkAllNotificationsAsReadCommandValidator : AbstractValidator<MarkAllNotificationsAsReadCommand>
{
    public MarkAllNotificationsAsReadCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("CompanyId é obrigatório.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");
    }
}

public class MarkAllNotificationsAsReadUseCase : IMarkAllNotificationsAsReadUseCase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<MarkAllNotificationsAsReadCommand> _validator;

    public MarkAllNotificationsAsReadUseCase(
        INotificationService notificationService,
        IValidator<MarkAllNotificationsAsReadCommand> validator)
    {
        _notificationService = notificationService;
        _validator = validator;
    }

    public async Task<Result<int>> ExecuteAsync(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result<int>.Failure("Dados inválidos para marcar todas notificações como lidas.", "VALIDATION_ERROR", errors);
        }

        return await _notificationService.MarkAllAsReadAsync(command.CompanyId, command.UserId, cancellationToken);
    }
}
