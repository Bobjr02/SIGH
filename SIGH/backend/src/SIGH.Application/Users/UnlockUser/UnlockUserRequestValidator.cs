using FluentValidation;

namespace SIGH.Application.Users.UnlockUser;

public class UnlockUserRequestValidator : AbstractValidator<UnlockUserRequest>
{
    public UnlockUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID do usuário a ser desbloqueado é obrigatório.");

        RuleFor(x => x.UnlockedByUserId)
            .NotEmpty().WithMessage("ID do administrador responsável pelo desbloqueio é obrigatório.");
    }
}
