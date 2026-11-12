using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public class AddEvidenceValidator : AbstractValidator<AddEvidenceRequest>
{
    public AddEvidenceValidator()
    {
        RuleFor(x => x.DisciplinaryCaseId)
            .NotEmpty().WithMessage("O processo disciplinar é obrigatório.");

        RuleFor(x => x.EvidenceType)
            .IsInEnum().WithMessage("O tipo de evidência informado é inválido.")
            .NotEqual(EvidenceType.Undefined).WithMessage("O tipo de evidência informado é inválido.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título da evidência é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.EvidenceTitleMaxLength).WithMessage($"O título deve ter no máximo {DisciplinaryDomainConstants.EvidenceTitleMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição da evidência textual é obrigatória.")
            .When(x => x.EvidenceType == EvidenceType.Text);

        RuleFor(x => x.Description)
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength).WithMessage($"A descrição deve ter no máximo {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CollectedByUserId)
            .NotEmpty().WithMessage("O usuário coletor é obrigatório.");

        RuleFor(x => x.CollectedAt)
            .NotEmpty().WithMessage("A data de coleta é obrigatória.");
    }
}
