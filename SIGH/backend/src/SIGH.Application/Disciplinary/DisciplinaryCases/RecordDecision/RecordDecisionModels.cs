using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public record RecordDecisionRequest(
    Guid DisciplinaryCaseId,
    DecisionType DecisionType,
    string Summary,
    string Reasoning,
    Guid DecidedByUserId);

public record RecordDecisionResponse(
    Guid DecisionId,
    Guid DisciplinaryCaseId,
    DecisionType DecisionType,
    string Summary,
    string Reasoning,
    DecisionStatus Status,
    DateTimeOffset DecidedAt,
    Guid DecidedByUserId,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt);
