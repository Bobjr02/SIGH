using FluentValidation;

namespace SIGH.Application.Employees.GetEmployees;

public class GetEmployeesValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("O número da página deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página deve estar entre 1 e 100.");
    }
}
