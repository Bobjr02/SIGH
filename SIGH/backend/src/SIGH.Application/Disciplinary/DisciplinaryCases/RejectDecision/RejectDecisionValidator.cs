using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;

public class RejectDecisionValidator : AbstractValidator<RejectDecisionRequest>
{
    public RejectDecisionValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O ID do processo disciplinar é obrigatório.");

        RuleFor(x => x.RejectedByUserId)
            .NotEmpty().WithMessage("O ID do usuário que rejeitou é obrigatório.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo da rejeição é obrigatório.");
    }
}
