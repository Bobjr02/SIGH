using FluentValidation;

namespace SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;

public class DeactivateInfractionTypeValidator : AbstractValidator<DeactivateInfractionTypeRequest>
{
    public DeactivateInfractionTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID do tipo de infração é obrigatório.");
    }
}
