using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;

public interface IGetDisciplinaryCasesUseCase
{
    Task<Result<PagedResult<DisciplinaryCaseSummaryDto>>> ExecuteAsync(GetDisciplinaryCasesQuery query, CancellationToken cancellationToken = default);
}
