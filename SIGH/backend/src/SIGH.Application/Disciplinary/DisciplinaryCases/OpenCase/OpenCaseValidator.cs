using FluentValidation;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;

public class OpenCaseValidator : AbstractValidator<OpenCaseRequest>
{
    public OpenCaseValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O ID do processo disciplinar é obrigatório.");

        RuleFor(x => x.OpenedByUserId)
            .NotEmpty().WithMessage("O usuário responsável pela abertura é obrigatório.");
    }
}
