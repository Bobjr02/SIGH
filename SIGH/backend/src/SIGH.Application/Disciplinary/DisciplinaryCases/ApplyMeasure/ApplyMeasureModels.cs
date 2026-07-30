using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public record ApplyMeasureRequest(
    Guid DisciplinaryCaseId,
    Guid DecisionId,
    Guid EmployeeId,
    DisciplinaryMeasureType Type,
    string Description,
    DateTimeOffset EffectiveFrom,
    Guid AppliedByUserId,
    DateTimeOffset? EffectiveUntil = null);

public record ApplyMeasureResponse(
    Guid MeasureId,
    Guid DisciplinaryCaseId,
    Guid DecisionId,
    Guid EmployeeId,
    DisciplinaryMeasureType Type,
    DisciplinaryMeasureStatus Status,
    DateTimeOffset AppliedAt);
