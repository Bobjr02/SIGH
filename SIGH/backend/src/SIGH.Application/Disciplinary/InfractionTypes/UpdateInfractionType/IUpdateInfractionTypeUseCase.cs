using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;

public interface IUpdateInfractionTypeUseCase
{
    Task<Result<UpdateInfractionTypeResponse>> ExecuteAsync(UpdateInfractionTypeRequest request, CancellationToken cancellationToken = default);
}
