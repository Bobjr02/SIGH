using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;

public class SubmitCaseForDecisionValidator : AbstractValidator<SubmitCaseForDecisionRequest>
{
    public SubmitCaseForDecisionValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O ID do processo disciplinar é obrigatório.");

        RuleFor(x => x.SubmittedByUserId)
            .NotEmpty().WithMessage("O usuário responsável pela submissão é obrigatório.");
    }
}
