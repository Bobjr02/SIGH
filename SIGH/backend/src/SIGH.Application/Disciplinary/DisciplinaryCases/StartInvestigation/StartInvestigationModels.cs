using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;

public record StartInvestigationRequest(Guid DisciplinaryCaseId, Guid InvestigatorUserId);
public record StartInvestigationResponse(Guid DisciplinaryCaseId, DisciplinaryCaseStatus Status);
