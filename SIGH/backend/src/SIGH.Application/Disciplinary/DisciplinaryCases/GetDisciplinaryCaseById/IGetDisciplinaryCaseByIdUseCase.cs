using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;

public interface IGetDisciplinaryCaseByIdUseCase
{
    Task<Result<GetDisciplinaryCaseByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
