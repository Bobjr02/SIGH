using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;

public record AddEmployeeToCaseRequest(
    Guid DisciplinaryCaseId,
    Guid EmployeeId,
    CaseEmployeeRole Role,
    bool IsPrimaryAccused = false,
    string? Notes = null);

public record AddEmployeeToCaseResponse(
    Guid Id,
    Guid DisciplinaryCaseId,
    Guid EmployeeId,
    CaseEmployeeRole Role,
    bool IsPrimaryAccused);
