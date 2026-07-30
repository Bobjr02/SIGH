using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;

namespace SIGH.Application.Employees.GetEmployeeById;

public class GetEmployeeByIdUseCase : IGetEmployeeByIdUseCase
{
    private readonly IEmployeeQueryService _queryService;

    public GetEmployeeByIdUseCase(IEmployeeQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<GetEmployeeByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return Result<GetEmployeeByIdResponse>.Failure("Funcionário não encontrado.", EmployeeErrors.NotFound);

        return Result<GetEmployeeByIdResponse>.Ok(result);
    }
}
