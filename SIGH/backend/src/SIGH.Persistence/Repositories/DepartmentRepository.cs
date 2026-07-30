using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly SighDbContext _context;

    public DepartmentRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == id, cancellationToken);
    }
}
