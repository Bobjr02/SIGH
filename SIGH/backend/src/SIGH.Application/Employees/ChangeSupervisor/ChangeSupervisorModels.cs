namespace SIGH.Application.Employees.ChangeSupervisor;

public record ChangeEmployeeSupervisorRequest(
    Guid EmployeeId,
    Guid? SupervisorId = null
);
