using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public class ApplyMeasureValidator : AbstractValidator<ApplyMeasureRequest>
{
    public ApplyMeasureValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.DecisionId)
            .NotEmpty().WithMessage("A decisão é obrigatória.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("O funcionário é obrigatório.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de medida disciplinar informado é inválido.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength).WithMessage($"A descrição deve ter no máximo {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("A data de início da vigência é obrigatória.");

        RuleFor(x => x.AppliedByUserId)
            .NotEmpty().WithMessage("O usuário aplicador é obrigatório.");
    }
}
