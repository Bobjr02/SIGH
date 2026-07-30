using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;

public record AddOccurrenceRequest(
    Guid DisciplinaryCaseId,
    DateTimeOffset OccurrenceDate,
    string Description,
    Guid ReportedByUserId,
    Guid InfractionTypeId,
    InfractionSeverity Severity,
    string? Location = null);

public record AddOccurrenceResponse(
    Guid OccurrenceId,
    Guid DisciplinaryCaseId,
    Guid InfractionTypeId,
    InfractionSeverity Severity,
    OccurrenceStatus Status);
