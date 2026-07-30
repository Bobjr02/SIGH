using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.CreateEmployee;

public interface ICreateEmployeeUseCase
{
    Task<Result<CreateEmployeeResponse>> ExecuteAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
}
