using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public class ApplyMeasureValidator : AbstractValidator<ApplyMeasureRequest>
{
    public ApplyMeasureValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.DisciplinaryDecisionId)
            .NotEmpty().WithMessage("A decisão é obrigatória.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("O funcionário é obrigatório.");

        RuleFor(x => x.MeasureType)
            .NotEqual(DisciplinaryMeasureType.Undefined)
            .WithMessage("O tipo de medida disciplinar informado é inválido.")
            .IsInEnum().WithMessage("O tipo de medida disciplinar informado é inválido.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A justificativa é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.ReasonMaxLength).WithMessage($"A justificativa deve ter no máximo {DisciplinaryDomainConstants.ReasonMaxLength} caracteres.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("A data de início da vigência é obrigatória.");

        RuleFor(x => x.AppliedByUserId)
            .NotEmpty().WithMessage("O usuário aplicador é obrigatório.");

        RuleFor(x => x.EffectiveUntil)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveUntil.HasValue)
            .WithMessage("A data final de vigência não pode ser anterior à data inicial.");

        RuleFor(x => x.Notes)
            .MaximumLength(DisciplinaryDomainConstants.NotesMaxLength)
            .When(x => x.Notes != null)
            .WithMessage($"As observações devem ter no máximo {DisciplinaryDomainConstants.NotesMaxLength} caracteres.");
    }
}
