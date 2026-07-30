using FluentValidation;

namespace SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;

public class ActivateInfractionTypeValidator : AbstractValidator<ActivateInfractionTypeRequest>
{
    public ActivateInfractionTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID do tipo de infração é obrigatório.");
    }
}
