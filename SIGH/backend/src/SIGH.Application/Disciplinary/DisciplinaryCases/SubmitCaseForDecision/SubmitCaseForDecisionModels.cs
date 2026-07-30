using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;

public record SubmitCaseForDecisionRequest(Guid DisciplinaryCaseId, Guid SubmittedByUserId);
public record SubmitCaseForDecisionResponse(Guid DisciplinaryCaseId, DisciplinaryCaseStatus Status);
