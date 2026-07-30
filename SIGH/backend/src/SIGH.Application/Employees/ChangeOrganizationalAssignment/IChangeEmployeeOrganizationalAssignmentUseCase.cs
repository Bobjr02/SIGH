using SIGH.Application.Common.Models;

namespace SIGH.Application.Employees.ChangeOrganizationalAssignment;

public interface IChangeEmployeeOrganizationalAssignmentUseCase
{
    Task<Result> ExecuteAsync(ChangeEmployeeOrganizationalAssignmentRequest request, CancellationToken cancellationToken = default);
}
