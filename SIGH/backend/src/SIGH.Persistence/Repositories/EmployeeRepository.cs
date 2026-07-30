using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Helpers;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly SighDbContext _context;

    public EmployeeRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCpf = CpfValidator.Normalize(cpf);
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Cpf == normalizedCpf && (!excludingEmployeeId.HasValue || e.Id != excludingEmployeeId.Value), cancellationToken);
    }

    public async Task<bool> ExistsByEmployeeNumberAsync(Guid companyId, string employeeNumber, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedNumber = employeeNumber.Trim();
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.CompanyId == companyId && e.EmployeeNumber == trimmedNumber && (!excludingEmployeeId.HasValue || e.Id != excludingEmployeeId.Value), cancellationToken);
    }

    public async Task<bool> ExistsByCorporateEmailAsync(string corporateEmail, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedEmail = corporateEmail.Trim();
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.CorporateEmail == trimmedEmail && (!excludingEmployeeId.HasValue || e.Id != excludingEmployeeId.Value), cancellationToken);
    }

    public async Task<bool> IsUserLinkedAsync(Guid userId, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && (!excludingEmployeeId.HasValue || e.Id != excludingEmployeeId.Value), cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }
}
