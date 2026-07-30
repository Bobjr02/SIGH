using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Interfaces.Repositories.Disciplinary;

public interface IDisciplinaryCaseRepository
{
    Task<DisciplinaryCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DisciplinaryCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCaseNumberAsync(Guid companyId, string caseNumber, CancellationToken cancellationToken = default);
    Task AddAsync(DisciplinaryCase disciplinaryCase, CancellationToken cancellationToken = default);
    Task<PagedResult<DisciplinaryCaseSummaryDto>> GetPagedAsync(GetDisciplinaryCasesQuery query, CancellationToken cancellationToken = default);
}
