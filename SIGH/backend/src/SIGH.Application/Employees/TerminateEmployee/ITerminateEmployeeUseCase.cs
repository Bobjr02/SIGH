using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.TerminateEmployee;

public interface ITerminateEmployeeUseCase
{
    Task<Result<TerminateEmployeeResponse>> ExecuteAsync(TerminateEmployeeRequest request, CancellationToken cancellationToken = default);
}
