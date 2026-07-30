using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;

public record GetDisciplinaryCasesQuery(
    Guid? CompanyId = null,
    DisciplinaryCaseStatus? Status = null,
    DisciplinaryCasePriority? Priority = null,
    Guid? ResponsibleEmployeeId = null,
    Guid? EmployeeId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10);
