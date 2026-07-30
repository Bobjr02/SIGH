using SIGH.Domain.Employees.Entities;

namespace SIGH.Domain.Repositories;

public interface IJobTitleRepository
{
    Task<JobTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
