using FluentValidation;

namespace SIGH.Application.Users.CreateUser;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(150).WithMessage("Nome completo não pode exceder 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail corporativo é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.")
            .MaximumLength(150).WithMessage("E-mail não pode exceder 150 caracteres.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Length(11).WithMessage("CPF deve conter exatamente 11 dígitos numéricos.");

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("A senha inicial é obrigatória.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("Atribua ao menos um perfil (Role) para o usuário.");
    }
}
