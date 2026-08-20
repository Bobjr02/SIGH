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

        RuleFor(x => x.StorageReference)
            .NotEmpty().WithMessage("A referência de armazenamento é obrigatória para evidências de arquivo.")
            .MaximumLength(DisciplinaryDomainConstants.StorageReferenceMaxLength).WithMessage($"A referência de armazenamento deve ter no máximo {DisciplinaryDomainConstants.StorageReferenceMaxLength} caracteres.")
            .When(x => x.EvidenceType != EvidenceType.Text);

        RuleFor(x => x.OriginalFileName)
            .NotEmpty().WithMessage("O nome original do arquivo é obrigatório para evidências de arquivo.")
            .MaximumLength(DisciplinaryDomainConstants.OriginalFileNameMaxLength).WithMessage($"O nome original do arquivo deve ter no máximo {DisciplinaryDomainConstants.OriginalFileNameMaxLength} caracteres.")
            .When(x => x.EvidenceType != EvidenceType.Text);

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("O tipo de conteúdo é obrigatório para evidências de arquivo.")
            .MaximumLength(DisciplinaryDomainConstants.ContentTypeMaxLength).WithMessage($"O tipo de conteúdo deve ter no máximo {DisciplinaryDomainConstants.ContentTypeMaxLength} caracteres.")
            .When(x => x.EvidenceType != EvidenceType.Text);

        RuleFor(x => x.FileSize)
            .NotNull().WithMessage("O tamanho do arquivo é obrigatório para evidências de arquivo.")
            .GreaterThanOrEqualTo(0).WithMessage("O tamanho do arquivo não pode ser negativo.")
            .When(x => x.EvidenceType != EvidenceType.Text);

        RuleFor(x => x.IntegrityHash)
            .MaximumLength(DisciplinaryDomainConstants.IntegrityHashMaxLength).WithMessage($"O hash de integridade deve ter no máximo {DisciplinaryDomainConstants.IntegrityHashMaxLength} caracteres.")
            .When(x => !string.IsNullOrEmpty(x.IntegrityHash));
    }
}
