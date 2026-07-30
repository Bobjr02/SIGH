using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;

public interface IActivateInfractionTypeUseCase
{
    Task<Result<ActivateInfractionTypeResponse>> ExecuteAsync(ActivateInfractionTypeRequest request, CancellationToken cancellationToken = default);
}
