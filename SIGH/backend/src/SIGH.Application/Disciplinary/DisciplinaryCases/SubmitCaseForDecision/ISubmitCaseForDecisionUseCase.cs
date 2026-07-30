using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;

public interface ISubmitCaseForDecisionUseCase
{
    Task<Result<SubmitCaseForDecisionResponse>> ExecuteAsync(SubmitCaseForDecisionRequest request, CancellationToken cancellationToken = default);
}
