using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryMeasure : AuditableEntity
{
    public Guid DisciplinaryCaseId { get; private set; }
    public Guid DisciplinaryDecisionId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DisciplinaryMeasureType MeasureType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset? AppliedAt { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveUntil { get; private set; }
    public Guid? AppliedByUserId { get; private set; }
    public DisciplinaryMeasureStatus Status { get; private set; } = DisciplinaryMeasureStatus.Pending;
    public string? Notes { get; private set; }

    // EF Core
    protected DisciplinaryMeasure() : base() { }

    protected DisciplinaryMeasure(Guid id) : base(id) { }

    public static DisciplinaryMeasure Create(
        Guid disciplinaryCaseId,
        Guid disciplinaryDecisionId,
        Guid employeeId,
        DisciplinaryMeasureType measureType,
        string reason,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil = null,
        string? notes = null)
    {
        if (disciplinaryCaseId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do processo disciplinar é obrigatório.");

        if (disciplinaryDecisionId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da decisão disciplinar é obrigatório.");

        if (employeeId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do funcionário é obrigatório.");

        if (measureType == DisciplinaryMeasureType.Undefined || !Enum.IsDefined(measureType))
            throw new BusinessRuleValidationException("O tipo de medida disciplinar é inválido.");

        if (effectiveFrom == default)
            throw new BusinessRuleValidationException("A data de início da vigência é inválida.");

        if (effectiveUntil.HasValue && effectiveUntil.Value == default)
            throw new BusinessRuleValidationException("A data final de vigência é inválida.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleValidationException("A justificativa da medida disciplinar é obrigatória.");

        var trimmedReason = reason.Trim();
        if (trimmedReason.Length > DisciplinaryDomainConstants.ReasonMaxLength)
            throw new BusinessRuleValidationException($"A justificativa da medida não pode exceder {DisciplinaryDomainConstants.ReasonMaxLength} caracteres.");

        if (effectiveUntil.HasValue && effectiveUntil.Value < effectiveFrom)
            throw new BusinessRuleValidationException("A data final de vigência não pode ser anterior à data inicial.");

        if (measureType == DisciplinaryMeasureType.Suspension && !effectiveUntil.HasValue)
            throw new BusinessRuleValidationException("A medida disciplinar de suspensão deve possuir uma data final de vigência.");

        if (notes?.Length > DisciplinaryDomainConstants.NotesMaxLength)
            throw new BusinessRuleValidationException($"As observações não podem exceder {DisciplinaryDomainConstants.NotesMaxLength} caracteres.");

        return new DisciplinaryMeasure
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            DisciplinaryDecisionId = disciplinaryDecisionId,
            EmployeeId = employeeId,
            MeasureType = measureType,
            Reason = trimmedReason,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = DisciplinaryMeasureStatus.Pending
        };
    }

    internal void Apply(DateTimeOffset appliedAt, Guid appliedByUserId)
    {
        if (appliedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário responsável pela aplicação é obrigatório.");

        if (appliedAt == default)
            throw new BusinessRuleValidationException("A data de aplicação da medida é inválida.");

        if (Status != DisciplinaryMeasureStatus.Pending)
            throw new BusinessRuleValidationException("Apenas medidas pendentes podem ser aplicadas.");

        AppliedAt = appliedAt;
        AppliedByUserId = appliedByUserId;
        Status = DisciplinaryMeasureStatus.Applied;
    }

    internal void Complete()
    {
        if (Status != DisciplinaryMeasureStatus.Applied)
            throw new BusinessRuleValidationException("Apenas medidas aplicadas podem ser concluídas.");

        Status = DisciplinaryMeasureStatus.Completed;
    }

    internal void Cancel(string reason)
    {
        if (Status != DisciplinaryMeasureStatus.Pending)
            throw new BusinessRuleValidationException("Apenas medidas disciplinares no estado Pendente podem ser canceladas.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleValidationException("A justificativa do cancelamento é obrigatória.");

        Status = DisciplinaryMeasureStatus.Cancelled;
    }

    internal void UpdateNotes(string notes)
    {
        if (notes?.Length > DisciplinaryDomainConstants.NotesMaxLength)
            throw new BusinessRuleValidationException($"As observações não podem exceder {DisciplinaryDomainConstants.NotesMaxLength} caracteres.");

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
