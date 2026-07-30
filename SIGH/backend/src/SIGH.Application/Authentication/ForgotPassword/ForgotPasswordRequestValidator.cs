using FluentValidation;

namespace SIGH.Application.Authentication.ForgotPassword;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail corporativo é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");
    }
}
