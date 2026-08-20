using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;

public class AddEmployeeToCaseValidator : AbstractValidator<AddEmployeeToCaseRequest>
{
    public AddEmployeeToCaseValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("O funcionário é obrigatório.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("O papel do funcionário no processo é inválido.");
    }
}
