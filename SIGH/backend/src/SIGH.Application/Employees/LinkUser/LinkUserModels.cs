namespace SIGH.Application.Employees.LinkUser;

public record LinkEmployeeUserRequest(
    Guid EmployeeId,
    Guid UserId
);
