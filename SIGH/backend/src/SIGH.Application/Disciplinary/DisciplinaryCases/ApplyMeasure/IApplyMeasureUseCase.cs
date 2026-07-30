using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public interface IApplyMeasureUseCase
{
    Task<Result<ApplyMeasureResponse>> ExecuteAsync(ApplyMeasureRequest request, CancellationToken cancellationToken = default);
}
