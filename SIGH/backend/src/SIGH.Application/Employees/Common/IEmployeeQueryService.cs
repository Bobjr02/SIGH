using SIGH.Application.Common.Models;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;

namespace SIGH.Application.Employees.Common;

public interface IEmployeeQueryService
{
    Task<GetEmployeeByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeListItemResponse>> SearchAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gerências e setores ativos, para os filtros da lista de funcionários.</summary>
    Task<EmployeeFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
}

public sealed record OrganizationOptionDto(Guid Id, string? Code, string Name, Guid? ManagementUnitId = null);

public sealed record EmployeeFilterOptionsDto(IReadOnlyList<OrganizationOptionDto> ManagementUnits, IReadOnlyList<OrganizationOptionDto> Departments);
