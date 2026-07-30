using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public record RecordDecisionRequest(
    Guid DisciplinaryCaseId,
    DecisionType Type,
    string Justification,
    Guid DecidedByUserId,
    Guid? ApprovedByUserId = null,
    DateTimeOffset? ApprovedAt = null);

public record RecordDecisionResponse(
    Guid DecisionId,
    Guid DisciplinaryCaseId,
    DecisionType Type,
    DecisionStatus Status,
    DateTimeOffset DecidedAt);
