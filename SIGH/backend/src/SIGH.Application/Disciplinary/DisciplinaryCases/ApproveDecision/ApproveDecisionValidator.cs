using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;

public class ApproveDecisionValidator : AbstractValidator<ApproveDecisionRequest>
{
    public ApproveDecisionValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O ID do processo disciplinar é obrigatório.");

        RuleFor(x => x.ApprovedByUserId)
            .NotEmpty().WithMessage("O ID do aprovador é obrigatório.");
    }
}
