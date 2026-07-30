using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.GetEmployees;

public record GetEmployeesQuery(
    Guid? CompanyId = null,
    Guid? ManagementUnitId = null,
    Guid? DepartmentId = null,
    Guid? JobTitleId = null,
    EmployeeStatus? Status = null,
    string? Search = null,
    bool? HasSystemAccess = null,
    DateOnly? AdmissionDateFrom = null,
    DateOnly? AdmissionDateTo = null,
    Guid? SupervisorId = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = "FullName",
    bool SortDescending = false,
    string? SearchTerm = null
)
{
    public string? EffectiveSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm) ? SearchTerm : Search;
};

public record EmployeeListItemResponse(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string EmployeeNumber,
    string FullName,
    string? SocialName,
    string CpfMasked,
    DateOnly AdmissionDate,
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
    DateTimeOffset CreatedAt
);
