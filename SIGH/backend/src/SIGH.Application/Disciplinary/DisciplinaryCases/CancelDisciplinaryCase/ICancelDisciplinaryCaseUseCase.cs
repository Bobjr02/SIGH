using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public interface ICancelDisciplinaryCaseUseCase
{
    Task<Result<CancelDisciplinaryCaseResponse>> ExecuteAsync(CancelDisciplinaryCaseRequest request, CancellationToken cancellationToken = default);
}
