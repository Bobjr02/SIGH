using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;

public class AddOccurrenceValidator : AbstractValidator<AddOccurrenceRequest>
{
    public AddOccurrenceValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.OccurrenceDate)
            .NotEmpty().WithMessage("A data da ocorrência é obrigatória.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength).WithMessage($"A descrição deve ter no máximo {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.ReportedByUserId)
            .NotEmpty().WithMessage("O usuário relator é obrigatório.");

        RuleFor(x => x.InfractionTypeId)
            .NotEmpty().WithMessage("O tipo de infração é obrigatório.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("A gravidade informada é inválida.");

        RuleFor(x => x.Location)
            .MaximumLength(DisciplinaryDomainConstants.LocationMaxLength)
            .When(x => !string.IsNullOrEmpty(x.Location));
    }
}
