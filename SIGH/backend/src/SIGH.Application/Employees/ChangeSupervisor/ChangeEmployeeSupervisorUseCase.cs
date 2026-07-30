using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.ChangeSupervisor;

public class ChangeEmployeeSupervisorUseCase : IChangeEmployeeSupervisorUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;

    public ChangeEmployeeSupervisorUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> ExecuteAsync(ChangeEmployeeSupervisorRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        if (request.SupervisorId.HasValue && request.SupervisorId.Value != Guid.Empty)
        {
            if (request.SupervisorId.Value == employee.Id)
                return Result.FailureResult("O funcionário não pode ser gestor de si mesmo.", EmployeeErrors.InvalidData);

            var supervisor = await _employeeRepository.GetByIdAsync(request.SupervisorId.Value, cancellationToken);
            if (supervisor == null)
                return Result.FailureResult("Gestor não encontrado.", EmployeeErrors.SupervisorNotFound);

            if (supervisor.CompanyId != employee.CompanyId)
                return Result.FailureResult("O gestor deve pertencer à mesma empresa do funcionário.", EmployeeErrors.SupervisorFromAnotherCompany);
        }

        try
        {
            employee.ChangeSupervisor(request.SupervisorId);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.FailureResult(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Gestor imediato alterado com sucesso.");
    }
}
