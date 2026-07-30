using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.GetEmployeeById;

public record GetEmployeeByIdRequest(Guid Id);

public record GetEmployeeByIdResponse(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string EmployeeNumber,
    string FullName,
    string? SocialName,
    string CpfMasked,
    DateOnly AdmissionDate,
    DateOnly? BirthDate,
    EmployeeStatus Status,
    Guid JobTitleId,
    string? JobTitleName,
    Guid ManagementUnitId,
    string? ManagementUnitName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? SupervisorId,
    string? SupervisorName,
    Guid? UserId,
    bool HasSystemAccess,
    string? CorporateEmail,
    string? PersonalEmail,
    string? MobileNumber,
    DateOnly? TerminationDate,
    string? TerminationReason,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
