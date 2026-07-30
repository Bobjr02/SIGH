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

        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        Status = DecisionStatus.Approved;
    }

    internal void Reject(string reason)
    {
        if (Status != DecisionStatus.PendingApproval && Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A decisão não está em um estado que permita rejeição.");

        Status = DecisionStatus.Rejected;
    }

    internal void Invalidate(string reason)
    {
        if (Status != DecisionStatus.Approved)
            throw new BusinessRuleValidationException("Apenas decisões aprovadas podem ser invalidadas.");

        Status = DecisionStatus.Invalidated;
    }
}
