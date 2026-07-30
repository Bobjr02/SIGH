using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public record CancelDisciplinaryCaseRequest(
    Guid DisciplinaryCaseId,
    Guid CancelledByUserId,
    string Reason);

public record CancelDisciplinaryCaseResponse(
    Guid DisciplinaryCaseId,
    DisciplinaryCaseStatus Status,
    DateTimeOffset CancelledAt,
    string Reason);
