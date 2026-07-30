using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;

public interface IApproveDecisionUseCase
{
    Task<Result<ApproveDecisionResponse>> ExecuteAsync(ApproveDecisionRequest request, CancellationToken cancellationToken = default);
}
