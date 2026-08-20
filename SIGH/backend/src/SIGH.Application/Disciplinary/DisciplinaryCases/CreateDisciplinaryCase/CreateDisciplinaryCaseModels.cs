using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;

public record CreateDisciplinaryCaseRequest(
    string CaseNumber,
    Guid CompanyId,
    string Title,
    string Description,
    Guid CreatedByUserId,
    DisciplinaryCasePriority Priority = DisciplinaryCasePriority.Normal,
    Guid? ResponsibleEmployeeId = null,
    DateTimeOffset? DueDate = null);

public record CreateDisciplinaryCaseResponse(
    Guid Id,
    string CaseNumber,
    Guid CompanyId,
    string Title,
    DisciplinaryCaseStatus Status,
    DisciplinaryCasePriority Priority,
    DateTimeOffset OpenedAt,
    Guid OpenedByUserId,
    Guid? ResponsibleEmployeeId,
    DateTimeOffset? DueDate);
