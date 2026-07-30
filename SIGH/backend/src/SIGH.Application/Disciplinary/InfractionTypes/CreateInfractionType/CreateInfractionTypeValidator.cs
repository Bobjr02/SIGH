using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;

public class CreateInfractionTypeValidator : AbstractValidator<CreateInfractionTypeRequest>
{
    public CreateInfractionTypeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("O código do tipo de infração é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.CodeMaxLength).WithMessage($"O código deve ter no máximo {DisciplinaryDomainConstants.CodeMaxLength} caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do tipo de infração é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.NameMaxLength).WithMessage($"O nome deve ter no máximo {DisciplinaryDomainConstants.NameMaxLength} caracteres.");

        RuleFor(x => x.DefaultSeverity)
            .IsInEnum().WithMessage("A gravidade padrão informada é inválida.");

        RuleFor(x => x.Description)
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.LegalReference)
            .MaximumLength(DisciplinaryDomainConstants.LegalReferenceMaxLength)
            .When(x => !string.IsNullOrEmpty(x.LegalReference));
    }
}
