using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class ManagementUnitRepository : IManagementUnitRepository
{
    private readonly SighDbContext _context;

    public ManagementUnitRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<ManagementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ManagementUnits
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ManagementUnits
            .AsNoTracking()
            .AnyAsync(m => m.Id == id, cancellationToken);
    }
}
