using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.LinkUser;

public interface ILinkEmployeeUserUseCase
{
    Task<Result> ExecuteAsync(LinkEmployeeUserRequest request, CancellationToken cancellationToken = default);
}
