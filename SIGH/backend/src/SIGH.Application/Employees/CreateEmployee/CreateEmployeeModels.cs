using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.CreateEmployee;

public record CreateEmployeeRequest(
    Guid CompanyId,
    string EmployeeNumber,
    string FullName,
    string Cpf,
    DateOnly AdmissionDate,
    Guid JobTitleId,
    Guid ManagementUnitId,
    EmployeeStatus InitialStatus = EmployeeStatus.PendingAdmission,
    string? SocialName = null,
    DateOnly? BirthDate = null,
    Guid? DepartmentId = null,
    Guid? SupervisorId = null,
    Guid? UserId = null,
    string? CorporateEmail = null,
    string? PersonalEmail = null,
    string? MobileNumber = null,
    string? Notes = null
);

public record CreateEmployeeResponse(
    Guid Id,
    Guid CompanyId,
    string EmployeeNumber,
    string FullName,
    string? SocialName,
    string CpfMasked,
    DateOnly AdmissionDate,
    EmployeeStatus Status,
    Guid JobTitleId,
    Guid ManagementUnitId,
    Guid? DepartmentId,
    Guid? SupervisorId,
    Guid? UserId,
    bool HasSystemAccess
);
