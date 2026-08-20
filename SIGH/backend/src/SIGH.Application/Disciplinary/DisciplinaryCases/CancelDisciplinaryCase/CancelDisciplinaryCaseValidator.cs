using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public class CancelDisciplinaryCaseValidator : AbstractValidator<CancelDisciplinaryCaseRequest>
{
    public CancelDisciplinaryCaseValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("O motivo do cancelamento é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.CancellationReasonMaxLength).WithMessage($"O motivo deve ter no máximo {DisciplinaryDomainConstants.CancellationReasonMaxLength} caracteres.");
    }
}
