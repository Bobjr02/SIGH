using SIGH.Application.Disciplinary.DTOs;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;

public record GetDisciplinaryCaseByIdResponse(
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
