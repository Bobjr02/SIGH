using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryOccurrence : AuditableEntity
{
    public Guid DisciplinaryCaseId { get; private set; }
    public DateTimeOffset OccurrenceDate { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }
    public string? Location { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid ReportedByUserId { get; private set; }
    public Guid InfractionTypeId { get; private set; }
    public InfractionSeverity Severity { get; private set; }
    public OccurrenceStatus Status { get; private set; } = OccurrenceStatus.Reported;
    public ConfidentialityLevel ConfidentialityLevel { get; private set; } = ConfidentialityLevel.Internal;

    // EF Core
    protected DisciplinaryOccurrence() : base() { }

    protected DisciplinaryOccurrence(Guid id) : base(id) { }

    public static DisciplinaryOccurrence Create(
        Guid disciplinaryCaseId,
        DateTimeOffset occurrenceDate,
        DateTimeOffset reportedAt,
        string description,
        Guid reportedByUserId,
        Guid infractionTypeId,
        InfractionSeverity severity,
        string? location = null,
        ConfidentialityLevel confidentialityLevel = ConfidentialityLevel.Internal)
    {
        if (disciplinaryCaseId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do processo disciplinar é obrigatório.");

        if (reportedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário relator é obrigatório.");

        if (infractionTypeId == Guid.Empty)
            throw new BusinessRuleValidationException("O tipo de infração é obrigatório.");

        if (occurrenceDate == default)
            throw new BusinessRuleValidationException("A data da ocorrência é inválida.");

        if (reportedAt == default)
            throw new BusinessRuleValidationException("A data do relato é inválida.");

        if (severity == InfractionSeverity.Undefined || !Enum.IsDefined(severity))
            throw new BusinessRuleValidationException("A severidade da infração é inválida.");

        if (confidentialityLevel == ConfidentialityLevel.Undefined || !Enum.IsDefined(confidentialityLevel))
            throw new BusinessRuleValidationException("O nível de confidencialidade é inválido.");

        if (occurrenceDate > reportedAt)
            throw new BusinessRuleValidationException("A data da ocorrência não pode ser posterior à data do relato.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("A descrição da ocorrência é obrigatória.");

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição da ocorrência não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        if (location?.Length > DisciplinaryDomainConstants.LocationMaxLength)
            throw new BusinessRuleValidationException($"A localização não pode exceder {DisciplinaryDomainConstants.LocationMaxLength} caracteres.");

        return new DisciplinaryOccurrence
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            OccurrenceDate = occurrenceDate,
            ReportedAt = reportedAt,
            Description = trimmedDescription,
            ReportedByUserId = reportedByUserId,
            InfractionTypeId = infractionTypeId,
            Severity = severity,
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            ConfidentialityLevel = confidentialityLevel,
            Status = OccurrenceStatus.Reported
        };
    }

    internal void UpdateDetails(string description, string? location = null, ConfidentialityLevel confidentialityLevel = ConfidentialityLevel.Internal)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("A descrição da ocorrência é obrigatória.");

        if (confidentialityLevel == ConfidentialityLevel.Undefined || !Enum.IsDefined(confidentialityLevel))
            throw new BusinessRuleValidationException("O nível de confidencialidade é inválido.");

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição da ocorrência não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        if (location?.Length > DisciplinaryDomainConstants.LocationMaxLength)
            throw new BusinessRuleValidationException($"A localização não pode exceder {DisciplinaryDomainConstants.LocationMaxLength} caracteres.");

        Description = trimmedDescription;
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        ConfidentialityLevel = confidentialityLevel;
    }

    internal void ChangeInfractionType(Guid infractionTypeId)
    {
        if (infractionTypeId == Guid.Empty)
            throw new BusinessRuleValidationException("O tipo de infração é obrigatório.");

        InfractionTypeId = infractionTypeId;
    }

    internal void ChangeSeverity(InfractionSeverity severity)
    {
        if (severity == InfractionSeverity.Undefined || !Enum.IsDefined(severity))
            throw new BusinessRuleValidationException("A severidade da infração é inválida.");

        Severity = severity;
    }

    internal void MarkAsValidated()
    {
        Status = OccurrenceStatus.Validated;
    }

    internal void MarkAsRejected()
    {
        Status = OccurrenceStatus.Rejected;
    }
}
