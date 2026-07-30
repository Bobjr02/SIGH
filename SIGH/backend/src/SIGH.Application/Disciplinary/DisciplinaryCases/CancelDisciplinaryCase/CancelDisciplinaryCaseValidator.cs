using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public class CancelDisciplinaryCaseValidator : AbstractValidator<CancelDisciplinaryCaseRequest>
{
    public CancelDisciplinaryCaseValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.CancelledByUserId)
            .NotEmpty().WithMessage("O usuário responsável pelo cancelamento é obrigatório.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo do cancelamento é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.ReasonMaxLength).WithMessage($"O motivo deve ter no máximo {DisciplinaryDomainConstants.ReasonMaxLength} caracteres.");
    }
}
