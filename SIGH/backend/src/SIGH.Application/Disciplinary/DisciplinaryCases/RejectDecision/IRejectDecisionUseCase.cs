using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;

public interface IRejectDecisionUseCase
{
    Task<Result<RejectDecisionResponse>> ExecuteAsync(RejectDecisionRequest request, CancellationToken cancellationToken = default);
}
