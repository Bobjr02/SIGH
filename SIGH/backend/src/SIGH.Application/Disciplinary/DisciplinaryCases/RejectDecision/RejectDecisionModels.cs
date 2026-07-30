using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;

public record RejectDecisionRequest(Guid DisciplinaryCaseId, Guid RejectedByUserId, string Reason);
public record RejectDecisionResponse(Guid DisciplinaryCaseId, Guid DecisionId, DisciplinaryCaseStatus Status, DecisionStatus DecisionStatus, string Reason);
