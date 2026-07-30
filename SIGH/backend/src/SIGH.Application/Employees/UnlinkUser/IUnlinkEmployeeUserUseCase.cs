using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.UnlinkUser;

public interface IUnlinkEmployeeUserUseCase
{
    Task<Result> ExecuteAsync(UnlinkEmployeeUserRequest request, CancellationToken cancellationToken = default);
}
