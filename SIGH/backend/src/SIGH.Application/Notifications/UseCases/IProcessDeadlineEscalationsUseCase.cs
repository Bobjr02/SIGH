namespace SIGH.Application.Notifications.UseCases;

using SIGH.Application.Common.Models;
using SIGH.Application.Notifications.DTOs;

public interface IProcessDeadlineEscalationsUseCase
{
    Task<Result<DeadlineProcessingResultDto>> ExecuteAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
}
