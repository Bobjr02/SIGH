using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;

public interface IGetInfractionTypesUseCase
{
    Task<Result<PagedResult<InfractionTypeDto>>> ExecuteAsync(GetInfractionTypesQuery query, CancellationToken cancellationToken = default);
}
