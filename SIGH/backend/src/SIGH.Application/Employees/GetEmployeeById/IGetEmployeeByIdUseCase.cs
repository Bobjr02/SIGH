using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.GetEmployeeById;

public interface IGetEmployeeByIdUseCase
{
    Task<Result<GetEmployeeByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
