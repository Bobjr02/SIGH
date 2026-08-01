using FluentValidation;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;

public class CreateDisciplinaryCaseValidator : AbstractValidator<CreateDisciplinaryCaseRequest>
{
    public CreateDisciplinaryCaseValidator()
    {
        RuleFor(x => x.CaseNumber)
            .NotEmpty().WithMessage("O número do processo é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.CaseNumberMaxLength).WithMessage($"O número do processo deve ter no máximo {DisciplinaryDomainConstants.CaseNumberMaxLength} caracteres.");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("A empresa é obrigatória.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título do processo é obrigatório.")
            .MaximumLength(DisciplinaryDomainConstants.TitleMaxLength).WithMessage($"O título deve ter no máximo {DisciplinaryDomainConstants.TitleMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(DisciplinaryDomainConstants.DescriptionMaxLength).WithMessage($"A descrição deve ter no máximo {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("O usuário criador é obrigatório.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("A prioridade é inválida.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTimeOffset.MinValue)
            .When(x => x.DueDate.HasValue)
            .WithMessage("A data de vencimento é inválida.");
    }
}
