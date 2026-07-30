namespace SIGH.Application.Employees.ChangeOrganizationalAssignment;

public record ChangeEmployeeOrganizationalAssignmentRequest(
    Guid EmployeeId,
    Guid JobTitleId,
    Guid ManagementUnitId,
    Guid? DepartmentId = null
);
