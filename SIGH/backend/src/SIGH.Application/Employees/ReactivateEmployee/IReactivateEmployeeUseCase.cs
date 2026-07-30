using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.ReactivateEmployee;

public interface IReactivateEmployeeUseCase
{
    Task<Result> ExecuteAsync(ReactivateEmployeeRequest request, CancellationToken cancellationToken = default);
}
