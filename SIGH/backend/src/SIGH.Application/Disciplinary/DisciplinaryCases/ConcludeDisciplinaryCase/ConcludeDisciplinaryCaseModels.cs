using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;

public record ConcludeDisciplinaryCaseRequest(
    Guid DisciplinaryCaseId,
    Guid ConcludedByUserId,
    string FinalSummary);

public record ConcludeDisciplinaryCaseResponse(
    Guid DisciplinaryCaseId,
    DisciplinaryCaseStatus Status,
    DateTimeOffset ClosedAt,
    string FinalSummary);
