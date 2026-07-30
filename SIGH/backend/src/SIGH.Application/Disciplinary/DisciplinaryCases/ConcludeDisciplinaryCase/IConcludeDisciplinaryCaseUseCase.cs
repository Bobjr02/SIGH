using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;

public interface IConcludeDisciplinaryCaseUseCase
{
    Task<Result<ConcludeDisciplinaryCaseResponse>> ExecuteAsync(ConcludeDisciplinaryCaseRequest request, CancellationToken cancellationToken = default);
}
