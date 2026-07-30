using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class JobTitleRepository : IJobTitleRepository
{
    private readonly SighDbContext _context;

    public JobTitleRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<JobTitle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobTitles
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobTitles
            .AsNoTracking()
            .AnyAsync(j => j.Id == id, cancellationToken);
    }
}
