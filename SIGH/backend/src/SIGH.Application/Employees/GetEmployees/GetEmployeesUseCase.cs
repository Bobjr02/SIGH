using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;

namespace SIGH.Application.Employees.GetEmployees;

public class GetEmployeesUseCase : IGetEmployeesUseCase
{
    private readonly IEmployeeQueryService _queryService;

    public GetEmployeesUseCase(IEmployeeQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<PagedResult<EmployeeListItemResponse>>> ExecuteAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _queryService.SearchAsync(query, cancellationToken);
        return Result<PagedResult<EmployeeListItemResponse>>.Ok(result);
    }
}
