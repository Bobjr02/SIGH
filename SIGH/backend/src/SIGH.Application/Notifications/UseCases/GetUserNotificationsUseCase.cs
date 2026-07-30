namespace SIGH.Application.Notifications.UseCases;

using FluentValidation;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.Queries;

public interface IGetUserNotificationsUseCase
{
    Task<Result<PagedResult<NotificationDto>>> ExecuteAsync(GetNotificationsQuery query, CancellationToken cancellationToken = default);
}

public class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("CompanyId é obrigatório.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("O número da página deve ser maior que zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("O tamanho da página deve ser maior que zero.")
            .LessThanOrEqualTo(100).WithMessage("O tamanho da página não pode ser superior a 100.");
    }
}

public class GetUserNotificationsUseCase : IGetUserNotificationsUseCase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<GetNotificationsQuery> _validator;

    public GetUserNotificationsUseCase(
        INotificationService notificationService,
        IValidator<GetNotificationsQuery> validator)
    {
        _notificationService = notificationService;
        _validator = validator;
    }

    public async Task<Result<PagedResult<NotificationDto>>> ExecuteAsync(GetNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result<PagedResult<NotificationDto>>.Failure("Dados inválidos para consulta de notificações.", "VALIDATION_ERROR", errors);
        }

        return await _notificationService.GetUserNotificationsAsync(
            query,
            cancellationToken);
    }
}
