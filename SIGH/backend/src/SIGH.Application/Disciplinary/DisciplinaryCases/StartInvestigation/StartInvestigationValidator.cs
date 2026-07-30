using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;

public class StartInvestigationValidator : AbstractValidator<StartInvestigationRequest>
{
    public StartInvestigationValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O ID do processo disciplinar é obrigatório.");

        RuleFor(x => x.InvestigatorUserId)
            .NotEmpty().WithMessage("O ID do investigador é obrigatório.");
    }
}
