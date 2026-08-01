using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public class RecordDecisionValidator : AbstractValidator<RecordDecisionRequest>
{
    public RecordDecisionValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.DecisionType)
            .IsInEnum().WithMessage("O tipo de decisão informado é inválido.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("O resumo da decisão é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.SummaryMaxLength).WithMessage($"O resumo da decisão deve ter no máximo {DisciplinaryDomainConstants.SummaryMaxLength} caracteres.");

        RuleFor(x => x.Reasoning)
            .NotEmpty().WithMessage("A fundamentação da decisão é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.ReasoningMaxLength).WithMessage($"A fundamentação da decisão deve ter no máximo {DisciplinaryDomainConstants.ReasoningMaxLength} caracteres.");

        RuleFor(x => x.DecidedByUserId)
            .NotEmpty().WithMessage("O usuário decisor é obrigatório.");
    }
}
