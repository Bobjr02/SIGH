using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;

public record AddEmployeeToCaseRequest(
    Guid DisciplinaryCaseId,
    Guid EmployeeId,
    CaseEmployeeRole Role,
    bool IsPrimarySubject = false);

public record AddEmployeeToCaseResponse(
    Guid Id,
    Guid DisciplinaryCaseId,
    Guid EmployeeId,
    CaseEmployeeRole Role,
    bool IsPrimarySubject);
