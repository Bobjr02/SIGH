using SIGH.Domain.Employees.Entities;

namespace SIGH.Domain.Repositories;

public interface IManagementUnitRepository
{
    Task<ManagementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
