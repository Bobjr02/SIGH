using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public record ApplyMeasureRequest(
    Guid DisciplinaryCaseId,
    Guid DisciplinaryDecisionId,
    Guid EmployeeId,
    DisciplinaryMeasureType MeasureType,
    string Reason,
    DateTimeOffset EffectiveFrom,
    Guid AppliedByUserId,
    DateTimeOffset? EffectiveUntil = null,
    string? Notes = null);

public record ApplyMeasureResponse(
    Guid MeasureId,
    Guid DisciplinaryCaseId,
    Guid DisciplinaryDecisionId,
    Guid EmployeeId,
    DisciplinaryMeasureType MeasureType,
    string Reason,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    Guid? AppliedByUserId,
    DateTimeOffset? AppliedAt,
    DisciplinaryMeasureStatus Status,
    string? Notes);
