using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Api.Contracts.Disciplinary;

// Contratos HTTP do módulo disciplinar.
// Nenhum destes modelos expõe o identificador do usuário que executa a ação:
// o ator é sempre obtido do token autenticado e injetado pelo controller.

public record CreateDisciplinaryCaseHttpRequest(
    string CaseNumber,
    Guid CompanyId,
    string Title,
    string Description,
    DisciplinaryCasePriority Priority = DisciplinaryCasePriority.Normal,
    Guid? ResponsibleEmployeeId = null,
    DateTimeOffset? DueDate = null)
{
    public CreateDisciplinaryCaseRequest ToApplicationRequest(Guid actorUserId) =>
        new(CaseNumber, CompanyId, Title, Description, actorUserId, Priority, ResponsibleEmployeeId, DueDate);
}

public record RejectDecisionHttpRequest(string Reason)
{
    public RejectDecisionRequest ToApplicationRequest(Guid caseId, Guid actorUserId) =>
        new(caseId, actorUserId, Reason);
}

public record AddOccurrenceHttpRequest(
    DateTimeOffset OccurrenceDate,
    string Description,
    Guid InfractionTypeId,
    InfractionSeverity Severity,
    string? Location = null)
{
    public AddOccurrenceRequest ToApplicationRequest(Guid caseId, Guid actorUserId) =>
        new(caseId, OccurrenceDate, Description, actorUserId, InfractionTypeId, Severity, Location);
}

public record AddEvidenceHttpRequest(
    EvidenceType EvidenceType,
    string Title,
    DateTimeOffset CollectedAt,
    Guid? OccurrenceId = null,
    string? Description = null)
{
    public AddEvidenceRequest ToApplicationRequest(Guid caseId, Guid actorUserId) =>
        new(caseId, EvidenceType, Title, actorUserId, CollectedAt, OccurrenceId, Description);
}

public record RecordDecisionHttpRequest(
    DecisionType DecisionType,
    string Summary,
    string Reasoning)
{
    public RecordDecisionRequest ToApplicationRequest(Guid caseId, Guid actorUserId) =>
        new(caseId, DecisionType, Summary, Reasoning, actorUserId);
}

public record ApplyMeasureHttpRequest(
    Guid DisciplinaryDecisionId,
    Guid EmployeeId,
    DisciplinaryMeasureType MeasureType,
    string Reason,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveUntil = null,
    string? Notes = null)
{
    public ApplyMeasureRequest ToApplicationRequest(Guid caseId, Guid actorUserId) =>
        new(caseId, DisciplinaryDecisionId, EmployeeId, MeasureType, Reason, EffectiveFrom, actorUserId, EffectiveUntil, Notes);
}
