namespace SIGH.Application.Employees.ReactivateEmployee;

public record ReactivateEmployeeRequest(
    Guid EmployeeId,
    DateOnly NewAdmissionDate
);
