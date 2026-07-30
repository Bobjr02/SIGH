using SIGH.Application.Common.Models;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;

namespace SIGH.Application.Employees.Common;

public interface IEmployeeQueryService
{
    Task<GetEmployeeByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeListItemResponse>> SearchAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default);
}
