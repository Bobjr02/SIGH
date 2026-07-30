using FluentValidation;

namespace SIGH.Application.Employees.TerminateEmployee;

public class TerminateEmployeeValidator : AbstractValidator<TerminateEmployeeRequest>
{
    public TerminateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("O ID do funcionário é obrigatório.");

        RuleFor(x => x.TerminationDate)
            .NotEmpty().WithMessage("A data de desligamento é obrigatória.");

        RuleFor(x => x.TerminationReason)
            .NotEmpty().WithMessage("O motivo do desligamento é obrigatório.")
            .MaximumLength(500).WithMessage("O motivo do desligamento deve ter no máximo 500 caracteres.");
    }
}
