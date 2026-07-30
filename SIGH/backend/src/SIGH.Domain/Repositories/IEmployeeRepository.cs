using SIGH.Domain.Employees.Entities;

namespace SIGH.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmployeeNumberAsync(Guid companyId, string employeeNumber, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCorporateEmailAsync(string corporateEmail, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUserLinkedAsync(Guid userId, Guid? excludingEmployeeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
}
