using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.ChangeStatus;

public record ChangeEmployeeStatusRequest(
    Guid EmployeeId,
    EmployeeStatus TargetStatus
);
