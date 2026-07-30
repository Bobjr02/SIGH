using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.ChangeStatus;

public interface IChangeEmployeeStatusUseCase
{
    Task<Result> ExecuteAsync(ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default);
}
