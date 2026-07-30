using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.ReactivateEmployee;

public class ReactivateEmployeeUseCase : IReactivateEmployeeUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;

    public ReactivateEmployeeUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> ExecuteAsync(ReactivateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        try
        {
            employee.Reactivate(request.NewAdmissionDate);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.FailureResult(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Funcionário reativado com sucesso.");
    }
}
