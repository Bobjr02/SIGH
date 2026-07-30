using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public class AddEvidenceValidator : AbstractValidator<AddEvidenceRequest>
{
    public AddEvidenceValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de evidência informado é inválido.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição da evidência é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength).WithMessage($"A descrição deve ter no máximo {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.CollectedByUserId)
            .NotEmpty().WithMessage("O usuário coletor é obrigatório.");

        RuleFor(x => x.CollectedAt)
            .NotEmpty().WithMessage("A data de coleta é obrigatória.");

        RuleFor(x => x.ReferenceCode)
            .MaximumLength(DisciplinaryDomainConstants.ReferenceCodeMaxLength)
            .When(x => !string.IsNullOrEmpty(x.ReferenceCode));

        RuleFor(x => x.Location)
            .MaximumLength(DisciplinaryDomainConstants.LocationMaxLength)
            .When(x => !string.IsNullOrEmpty(x.Location));
    }
}
