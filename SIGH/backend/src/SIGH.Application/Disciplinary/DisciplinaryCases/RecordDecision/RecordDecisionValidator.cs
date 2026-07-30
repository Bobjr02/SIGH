using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public class RecordDecisionValidator : AbstractValidator<RecordDecisionRequest>
{
    public RecordDecisionValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de decisão informado é inválido.");

        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("A justificativa é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.JustificationMaxLength).WithMessage($"A justificativa deve ter no máximo {DisciplinaryDomainConstants.JustificationMaxLength} caracteres.");

        RuleFor(x => x.DecidedByUserId)
            .NotEmpty().WithMessage("O usuário decisor é obrigatório.");
    }
}
