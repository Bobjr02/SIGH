using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;

public record CreateDisciplinaryCaseRequest(
    string CaseNumber,
    Guid CompanyId,
    string Title,
    string Description,
    Guid CreatedByUserId,
    DisciplinaryCasePriority Priority = DisciplinaryCasePriority.Medium,
    ConfidentialityLevel ConfidentialityLevel = ConfidentialityLevel.Internal);

public record CreateDisciplinaryCaseResponse(
    Guid Id,
    string CaseNumber,
    Guid CompanyId,
    string Title,
    DisciplinaryCaseStatus Status,
    DisciplinaryCasePriority Priority,
    ConfidentialityLevel ConfidentialityLevel,
    DateTimeOffset OpenedAt);
