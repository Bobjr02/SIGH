using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Interfaces.Repositories.Disciplinary;

public interface IInfractionTypeRepository
{
    Task<InfractionType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(InfractionType infractionType, CancellationToken cancellationToken = default);
    Task<PagedResult<InfractionTypeDto>> GetPagedAsync(GetInfractionTypesQuery query, CancellationToken cancellationToken = default);
}
