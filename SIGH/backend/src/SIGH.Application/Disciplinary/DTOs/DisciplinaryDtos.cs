using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DTOs;

public record InfractionTypeDto(
    Guid Id,
    string Code,
    string Name,
    InfractionSeverity DefaultSeverity,
    bool RequiresFormalInvestigation,
    bool AllowsTerminationRecommendation,
    string? Description,
    string? LegalReference,
    bool IsActive);

public record OccurrenceDto(
    Guid Id,
    DateTimeOffset OccurrenceDate,
    DateTimeOffset ReportedAt,
    string Description,
    Guid ReportedByUserId,
    Guid InfractionTypeId,
    InfractionSeverity Severity,
    string? Location,
    OccurrenceStatus Status);

public record CaseEmployeeDto(
    Guid Id,
    Guid EmployeeId,
    CaseEmployeeRole Role,
    bool IsPrimarySubject);

public record EvidenceDto(
    Guid Id,
    EvidenceType EvidenceType,
    string Title,
    string? Description,
    string? StorageReference,
    string? OriginalFileName,
    string? ContentType,
    long? FileSize,
    Guid? OccurrenceId,
    DateTimeOffset CollectedAt,
    Guid CollectedByUserId,
    string? IntegrityHash,
    EvidenceStatus Status);

public record DecisionDto(
    Guid Id,
    DecisionType DecisionType,
    string Summary,
    string Reasoning,
    Guid DecidedByUserId,
    DateTimeOffset DecidedAt,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    DecisionStatus Status);

public record MeasureDto(
    Guid Id,
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

public record DisciplinaryCaseSummaryDto(
    Guid Id,
    string CaseNumber,
    Guid CompanyId,
    string Title,
    DisciplinaryCaseStatus Status,
    DisciplinaryCasePriority Priority,
    DateTimeOffset OpenedAt,
    Guid OpenedByUserId,
    Guid? ResponsibleEmployeeId,
    DateTimeOffset? DueDate,
    DateTimeOffset? ClosedAt,
    int OccurrencesCount,
    int EmployeesCount);

public record DisciplinaryCaseDetailDto(
    Guid Id,
    string CaseNumber,
    Guid CompanyId,
    string Title,
    string Description,
    DisciplinaryCaseStatus Status,
    DisciplinaryCasePriority Priority,
    DateTimeOffset OpenedAt,
    Guid OpenedByUserId,
    Guid? ResponsibleEmployeeId,
    DateTimeOffset? DueDate,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    string? ConclusionSummary,
    IReadOnlyCollection<OccurrenceDto> Occurrences,
    IReadOnlyCollection<CaseEmployeeDto> Employees,
    IReadOnlyCollection<EvidenceDto> Evidences,
    IReadOnlyCollection<DecisionDto> Decisions,
    IReadOnlyCollection<MeasureDto> Measures);
