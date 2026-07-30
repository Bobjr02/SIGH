using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class InfractionType : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public InfractionSeverity DefaultSeverity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool RequiresFormalInvestigation { get; private set; }
    public bool AllowsTerminationRecommendation { get; private set; }
    public string? LegalReference { get; private set; }

    // EF Core
    protected InfractionType() : base() { }

    protected InfractionType(Guid id) : base(id) { }

    public static InfractionType Create(
        string code,
        string name,
        InfractionSeverity defaultSeverity,
        bool requiresFormalInvestigation = false,
        bool allowsTerminationRecommendation = false,
        string? description = null,
        string? legalReference = null)
    {
        if (defaultSeverity == InfractionSeverity.Undefined || !Enum.IsDefined(defaultSeverity))
            throw new BusinessRuleValidationException("A severidade padrão é inválida.");

        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleValidationException("O código do tipo de infração é obrigatório.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length > DisciplinaryDomainConstants.InfractionCodeMaxLength)
            throw new BusinessRuleValidationException($"O código do tipo de infração não pode exceder {DisciplinaryDomainConstants.InfractionCodeMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("O nome do tipo de infração é obrigatório.");

        var trimmedName = name.Trim();
        if (trimmedName.Length > DisciplinaryDomainConstants.InfractionNameMaxLength)
            throw new BusinessRuleValidationException($"O nome do tipo de infração não pode exceder {DisciplinaryDomainConstants.InfractionNameMaxLength} caracteres.");

        if (description?.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        if (legalReference?.Length > DisciplinaryDomainConstants.LegalReferenceMaxLength)
            throw new BusinessRuleValidationException($"A referência legal não pode exceder {DisciplinaryDomainConstants.LegalReferenceMaxLength} caracteres.");

        return new InfractionType
        {
            Code = normalizedCode,
            Name = trimmedName,
            DefaultSeverity = defaultSeverity,
            RequiresFormalInvestigation = requiresFormalInvestigation,
            AllowsTerminationRecommendation = allowsTerminationRecommendation,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            LegalReference = string.IsNullOrWhiteSpace(legalReference) ? null : legalReference.Trim(),
            IsActive = true
        };
    }

    public void UpdateDetails(
        string name,
        InfractionSeverity defaultSeverity,
        bool requiresFormalInvestigation,
        bool allowsTerminationRecommendation,
        string? description = null,
        string? legalReference = null)
    {
        if (defaultSeverity == InfractionSeverity.Undefined || !Enum.IsDefined(defaultSeverity))
            throw new BusinessRuleValidationException("A severidade padrão é inválida.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("O nome do tipo de infração é obrigatório.");

        var trimmedName = name.Trim();
        if (trimmedName.Length > DisciplinaryDomainConstants.InfractionNameMaxLength)
            throw new BusinessRuleValidationException($"O nome do tipo de infração não pode exceder {DisciplinaryDomainConstants.InfractionNameMaxLength} caracteres.");

        if (description?.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        if (legalReference?.Length > DisciplinaryDomainConstants.LegalReferenceMaxLength)
            throw new BusinessRuleValidationException($"A referência legal não pode exceder {DisciplinaryDomainConstants.LegalReferenceMaxLength} caracteres.");

        Name = trimmedName;
        DefaultSeverity = defaultSeverity;
        RequiresFormalInvestigation = requiresFormalInvestigation;
        AllowsTerminationRecommendation = allowsTerminationRecommendation;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        LegalReference = string.IsNullOrWhiteSpace(legalReference) ? null : legalReference.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
