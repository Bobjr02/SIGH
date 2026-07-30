using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.TerminateEmployee;

public record TerminateEmployeeRequest(
    Guid EmployeeId,
    DateOnly TerminationDate,
    string TerminationReason
);

public record TerminateEmployeeResponse(
    Guid EmployeeId,
    EmployeeStatus Status,
    DateOnly TerminationDate,
    string TerminationReason
);
