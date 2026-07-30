using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;

public interface ICreateDisciplinaryCaseUseCase
{
    Task<Result<CreateDisciplinaryCaseResponse>> ExecuteAsync(CreateDisciplinaryCaseRequest request, CancellationToken cancellationToken = default);
}
