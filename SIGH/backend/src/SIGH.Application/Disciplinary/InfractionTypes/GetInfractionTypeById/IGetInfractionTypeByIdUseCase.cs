using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;

public interface IGetInfractionTypeByIdUseCase
{
    Task<Result<GetInfractionTypeByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
