using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.ChangeSupervisor;

public interface IChangeEmployeeSupervisorUseCase
{
    Task<Result> ExecuteAsync(ChangeEmployeeSupervisorRequest request, CancellationToken cancellationToken = default);
}
