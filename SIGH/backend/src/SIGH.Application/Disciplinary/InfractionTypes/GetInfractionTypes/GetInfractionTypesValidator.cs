using FluentValidation;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;

public class GetInfractionTypesValidator : AbstractValidator<GetInfractionTypesQuery>
{
    public GetInfractionTypesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("O número da página deve ser maior que zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("O tamanho da página deve ser maior que zero.")
            .LessThanOrEqualTo(100).WithMessage("O tamanho máximo da página é 100.");
    }
}
