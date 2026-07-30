using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;

public interface ICreateInfractionTypeUseCase
{
    Task<Result<CreateInfractionTypeResponse>> ExecuteAsync(CreateInfractionTypeRequest request, CancellationToken cancellationToken = default);
}
