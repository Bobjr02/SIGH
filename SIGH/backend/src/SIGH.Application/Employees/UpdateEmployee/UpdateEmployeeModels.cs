using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.UpdateEmployee;

public record UpdateEmployeeRequest(
    Guid EmployeeId,
    string EmployeeNumber,
    string FullName,
    string Cpf,
    Guid JobTitleId,
    Guid ManagementUnitId,
    string? SocialName = null,
    DateOnly? BirthDate = null,
    Guid? DepartmentId = null,
    string? CorporateEmail = null,
    string? PersonalEmail = null,
    string? MobileNumber = null,
    string? Notes = null
);

public record UpdateEmployeeResponse(
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
    string? CorporateEmail,
    string? PersonalEmail,
    string? MobileNumber,
    string? Notes
);
