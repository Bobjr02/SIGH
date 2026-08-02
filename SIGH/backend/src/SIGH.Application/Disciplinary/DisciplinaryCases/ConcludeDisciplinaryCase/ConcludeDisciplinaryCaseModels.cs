using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;

public record ConcludeDisciplinaryCaseRequest(
    Guid DisciplinaryCaseId,
    string ConclusionSummary);

public record ConcludeDisciplinaryCaseResponse(
    Guid DisciplinaryCaseId,
    DisciplinaryCaseStatus Status,
    string ConclusionSummary,
    DateTimeOffset ClosedAt);
