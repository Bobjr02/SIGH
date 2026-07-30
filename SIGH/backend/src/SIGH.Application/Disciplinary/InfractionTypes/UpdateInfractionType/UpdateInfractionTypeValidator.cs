using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;

public class UpdateInfractionTypeValidator : AbstractValidator<UpdateInfractionTypeRequest>
{
    public UpdateInfractionTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID do tipo de infração é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do tipo de infração é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.InfractionNameMaxLength)
            .WithMessage($"O nome do tipo de infração não pode exceder {DisciplinaryDomainConstants.InfractionNameMaxLength} caracteres.");

        RuleFor(x => x.DefaultSeverity)
            .IsInEnum().WithMessage("A severidade padrão é inválida.");

        RuleFor(x => x.Description)
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength)
            .WithMessage($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.LegalReference)
            .MaximumLength(DisciplinaryDomainConstants.LegalReferenceMaxLength)
            .WithMessage($"A referência legal não pode exceder {DisciplinaryDomainConstants.LegalReferenceMaxLength} caracteres.");
    }
}
