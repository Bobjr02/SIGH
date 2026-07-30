using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;

public class ConcludeDisciplinaryCaseValidator : AbstractValidator<ConcludeDisciplinaryCaseRequest>
{
    public ConcludeDisciplinaryCaseValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.ConcludedByUserId)
            .NotEmpty().WithMessage("O usuário responsável pela conclusão é obrigatório.");

        RuleFor(x => x.FinalSummary)
            .NotEmpty().WithMessage("O resumo final é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.FinalSummaryMaxLength).WithMessage($"O resumo final deve ter no máximo {DisciplinaryDomainConstants.FinalSummaryMaxLength} caracteres.");
    }
}
