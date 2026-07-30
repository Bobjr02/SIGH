using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public interface IRecordDecisionUseCase
{
    Task<Result<RecordDecisionResponse>> ExecuteAsync(RecordDecisionRequest request, CancellationToken cancellationToken = default);
}
