using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.UpdateEmployee;

public interface IUpdateEmployeeUseCase
{
    Task<Result<UpdateEmployeeResponse>> ExecuteAsync(UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
}
