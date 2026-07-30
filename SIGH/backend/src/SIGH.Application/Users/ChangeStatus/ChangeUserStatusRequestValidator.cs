using FluentValidation;

namespace SIGH.Application.Users.ChangeStatus;

public class ChangeUserStatusRequestValidator : AbstractValidator<ChangeUserStatusRequest>
{
    public ChangeUserStatusRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID do usuário é obrigatório.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Status informado é inválido.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A justificativa para alteração de status é obrigatória.")
            .MaximumLength(250).WithMessage("A justificativa não pode exceder 250 caracteres.");
    }
}
