using FluentValidation;

namespace SIGH.Application.Employees.UpdateEmployee;

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("O ID do funcionário é obrigatório.");

        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("A matrícula é obrigatória.")
            .MaximumLength(20).WithMessage("A matrícula deve ter no máximo 20 caracteres.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("O nome completo é obrigatório.")
            .MaximumLength(150).WithMessage("O nome completo deve ter no máximo 150 caracteres.");

        RuleFor(x => x.SocialName)
            .MaximumLength(150).WithMessage("O nome social deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.");

        RuleFor(x => x.JobTitleId)
            .NotEmpty().WithMessage("O cargo é obrigatório.");

        RuleFor(x => x.ManagementUnitId)
            .NotEmpty().WithMessage("A unidade gestora é obrigatória.");

        RuleFor(x => x.CorporateEmail)
            .MaximumLength(150).WithMessage("O e-mail corporativo deve ter no máximo 150 caracteres.");

        RuleFor(x => x.PersonalEmail)
            .MaximumLength(150).WithMessage("O e-mail pessoal deve ter no máximo 150 caracteres.");

        RuleFor(x => x.MobileNumber)
            .MaximumLength(20).WithMessage("O celular deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("As observações devem ter no máximo 1000 caracteres.");
    }
}
