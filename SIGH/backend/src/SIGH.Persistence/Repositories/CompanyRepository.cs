using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly SighDbContext _context;

    public CompanyRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);
    }
}
