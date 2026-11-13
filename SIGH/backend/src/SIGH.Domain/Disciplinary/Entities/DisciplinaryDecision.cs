using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryDecision : AuditableEntity
{
    public Guid DisciplinaryCaseId { get; private set; }
    public DecisionType DecisionType { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Reasoning { get; private set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; private set; }
    public Guid DecidedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DecisionStatus Status { get; private set; } = DecisionStatus.Draft;

    // Medida proposta (fluxo simplificado do RH): o registrante informa a medida já decidida; ela só vira
    // DisciplinaryMeasure quando a decisão é aprovada.
    public DisciplinaryMeasureType? ProposedMeasureType { get; private set; }
    public DateOnly? MeasureDate { get; private set; }
    public DateOnly? SuspensionStart { get; private set; }
    public DateOnly? SuspensionEnd { get; private set; }

    // Rejeição: quem, quando e por quê (a decisão rejeitada fica na história do processo).
    public Guid? RejectedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>Dias de suspensão, contando o primeiro e o último dia.</summary>
    public int? SuspensionDays => SuspensionStart is { } start && SuspensionEnd is { } end
        ? end.DayNumber - start.DayNumber + 1
        : null;

    // EF Core
    protected DisciplinaryDecision() : base() { }

    protected DisciplinaryDecision(Guid id) : base(id) { }

    public static DisciplinaryDecision Create(
        Guid disciplinaryCaseId,
        DecisionType decisionType,
        string summary,
        string reasoning,
        DateTimeOffset decidedAt,
        Guid decidedByUserId,
        DecisionStatus initialStatus = DecisionStatus.Draft)
    {
        if (disciplinaryCaseId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do processo disciplinar é obrigatório.");

        if (decidedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário decisor é obrigatório.");

        if (decidedAt == default)
            throw new BusinessRuleValidationException("A data da decisão é inválida.");

        if (decisionType == DecisionType.Undefined || !Enum.IsDefined(decisionType))
            throw new BusinessRuleValidationException("O tipo de decisão é inválido.");

        if (initialStatus == DecisionStatus.Undefined || !Enum.IsDefined(initialStatus))
            throw new BusinessRuleValidationException("O status inicial da decisão é inválido.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new BusinessRuleValidationException("O resumo da decisão é obrigatório.");

        var trimmedSummary = summary.Trim();
        if (trimmedSummary.Length > DisciplinaryDomainConstants.SummaryMaxLength)
            throw new BusinessRuleValidationException($"O resumo da decisão não pode exceder {DisciplinaryDomainConstants.SummaryMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(reasoning))
            throw new BusinessRuleValidationException("A fundamentação da decisão é obrigatória.");

        var trimmedReasoning = reasoning.Trim();
        if (trimmedReasoning.Length > DisciplinaryDomainConstants.ReasoningMaxLength)
            throw new BusinessRuleValidationException($"A fundamentação da decisão não pode exceder {DisciplinaryDomainConstants.ReasoningMaxLength} caracteres.");

        return new DisciplinaryDecision
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            DecisionType = decisionType,
            Summary = trimmedSummary,
            Reasoning = trimmedReasoning,
            DecidedAt = decidedAt,
            DecidedByUserId = decidedByUserId,
            Status = initialStatus
        };
    }

    public bool RequiresApproval() => DecisionType != DecisionType.NoViolation;

    /// <summary>Tipo de decisão correspondente a uma medida do registro simplificado.</summary>
    public static DecisionType DecisionTypeFor(DisciplinaryMeasureType measureType) => measureType switch
    {
        DisciplinaryMeasureType.VerbalWarning or DisciplinaryMeasureType.WrittenWarning => DecisionType.FormalWarning,
        DisciplinaryMeasureType.Suspension => DecisionType.Suspension,
        DisciplinaryMeasureType.JustCauseTermination => DecisionType.JustCauseTermination,
        DisciplinaryMeasureType.TerminationRecommendation => DecisionType.TerminationRecommendation,
        _ => DecisionType.Other
    };

    /// <summary>
    /// Registra a medida já decidida que acompanha a decisão. Só no rascunho. Suspensão exige início e fim
    /// (fim igual ou posterior ao início); os outros tipos não têm período.
    /// </summary>
    public void ProposeMeasure(DisciplinaryMeasureType measureType, DateOnly measureDate, DateOnly? suspensionStart = null, DateOnly? suspensionEnd = null)
    {
        if (Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A medida proposta só pode ser definida enquanto a decisão é rascunho.");

        if (measureType == DisciplinaryMeasureType.Undefined || !Enum.IsDefined(measureType))
            throw new BusinessRuleValidationException("O tipo de medida disciplinar é inválido.");

        if (measureDate == default)
            throw new BusinessRuleValidationException("A data da medida é obrigatória.");

        if (measureType == DisciplinaryMeasureType.Suspension)
        {
            if (suspensionStart is null || suspensionEnd is null)
                throw new BusinessRuleValidationException("A suspensão exige data de início e de fim.");
            if (suspensionEnd < suspensionStart)
                throw new BusinessRuleValidationException("O fim da suspensão não pode ser anterior ao início.");
        }
        else if (suspensionStart is not null || suspensionEnd is not null)
        {
            throw new BusinessRuleValidationException("Somente a suspensão tem período de início e fim.");
        }

        ProposedMeasureType = measureType;
        MeasureDate = measureDate;
        SuspensionStart = suspensionStart;
        SuspensionEnd = suspensionEnd;
    }

    internal void UpdateBeforeApproval(DecisionType decisionType, string summary, string reasoning)
    {
        if (Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A decisão só pode ser alterada no estado Rascunho.");

        if (decisionType == DecisionType.Undefined || !Enum.IsDefined(decisionType))
            throw new BusinessRuleValidationException("O tipo de decisão é inválido.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new BusinessRuleValidationException("O resumo da decisão é obrigatório.");

        var trimmedSummary = summary.Trim();
        if (trimmedSummary.Length > DisciplinaryDomainConstants.SummaryMaxLength)
            throw new BusinessRuleValidationException($"O resumo da decisão não pode exceder {DisciplinaryDomainConstants.SummaryMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(reasoning))
            throw new BusinessRuleValidationException("A fundamentação da decisão é obrigatória.");

        var trimmedReasoning = reasoning.Trim();
        if (trimmedReasoning.Length > DisciplinaryDomainConstants.ReasoningMaxLength)
            throw new BusinessRuleValidationException($"A fundamentação da decisão não pode exceder {DisciplinaryDomainConstants.ReasoningMaxLength} caracteres.");

        DecisionType = decisionType;
        Summary = trimmedSummary;
        Reasoning = trimmedReasoning;
    }

    internal void SubmitForApproval()
    {
        if (Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A decisão só pode ser submetida para aprovação a partir do estado Rascunho.");

        Status = DecisionStatus.PendingApproval;
    }

    internal void Approve(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (approvedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário aprovador é obrigatório.");

        if (approvedAt == default)
            throw new BusinessRuleValidationException("A data de aprovação é inválida.");

        if (Status != DecisionStatus.PendingApproval && Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A decisão só pode ser aprovada se estiver em rascunho ou pendente de aprovação.");

        EnsureReviewerIsNotDecider(approvedByUserId, "aprovar");

        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        Status = DecisionStatus.Approved;
    }

    internal void Reject(Guid rejectedByUserId, string reason, DateTimeOffset? rejectedAt = null)
    {
        if (rejectedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário responsável pela rejeição é obrigatório.");

        if (Status != DecisionStatus.PendingApproval && Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A decisão não está em um estado que permita rejeição.");

        EnsureReviewerIsNotDecider(rejectedByUserId, "rejeitar");

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason?.Length > DisciplinaryDomainConstants.ReasonMaxLength)
            throw new BusinessRuleValidationException($"O motivo da rejeição não pode exceder {DisciplinaryDomainConstants.ReasonMaxLength} caracteres.");

        RejectedByUserId = rejectedByUserId;
        RejectedAt = rejectedAt ?? DateTimeOffset.UtcNow;
        RejectionReason = trimmedReason;
        Status = DecisionStatus.Rejected;
    }

    /// <summary>
    /// Segregação de funções: quem registrou a decisão não pode revisá-la (aprovar ou rejeitar).
    /// </summary>
    private void EnsureReviewerIsNotDecider(Guid reviewerUserId, string action)
    {
        if (reviewerUserId == DecidedByUserId)
            throw new BusinessRuleValidationException(
                $"O usuário que registrou a decisão não pode {action} a própria decisão.");
    }

    internal void Invalidate(string reason)
    {
        if (Status != DecisionStatus.Approved)
            throw new BusinessRuleValidationException("Apenas decisões aprovadas podem ser invalidadas.");

        Status = DecisionStatus.Invalidated;
    }
}
