using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public record CancelDisciplinaryCaseRequest(
    Guid DisciplinaryCaseId,
    string CancellationReason);

public record CancelDisciplinaryCaseResponse(
    Guid DisciplinaryCaseId,
    DisciplinaryCaseStatus Status,
    DateTimeOffset CancelledAt,
    string CancellationReason);
