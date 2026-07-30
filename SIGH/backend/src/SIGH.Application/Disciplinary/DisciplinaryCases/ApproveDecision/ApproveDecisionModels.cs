using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;

public record ApproveDecisionRequest(Guid DisciplinaryCaseId, Guid ApprovedByUserId);
public record ApproveDecisionResponse(Guid DisciplinaryCaseId, Guid DecisionId, DisciplinaryCaseStatus Status, DecisionStatus DecisionStatus, DateTimeOffset ApprovedAt);
