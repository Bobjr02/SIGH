using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;

public interface IDeactivateInfractionTypeUseCase
{
    Task<Result<DeactivateInfractionTypeResponse>> ExecuteAsync(DeactivateInfractionTypeRequest request, CancellationToken cancellationToken = default);
}
