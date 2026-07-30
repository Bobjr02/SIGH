using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;

public interface IStartInvestigationUseCase
{
    Task<Result<StartInvestigationResponse>> ExecuteAsync(StartInvestigationRequest request, CancellationToken cancellationToken = default);
}
