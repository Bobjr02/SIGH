using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.GetEmployees;

public interface IGetEmployeesUseCase
{
    Task<Result<PagedResult<EmployeeListItemResponse>>> ExecuteAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default);
}
