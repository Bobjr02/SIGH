using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.TerminateEmployee;

public class TerminateEmployeeUseCase : ITerminateEmployeeUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;

    public TerminateEmployeeUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<TerminateEmployeeResponse>> ExecuteAsync(TerminateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result<TerminateEmployeeResponse>.Failure("Funcionário não encontrado.", EmployeeErrors.NotFound);

        try
        {
            employee.Terminate(request.TerminationDate, request.TerminationReason);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<TerminateEmployeeResponse>.Failure(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new TerminateEmployeeResponse(
            employee.Id,
            employee.Status,
            employee.TerminationDate!.Value,
            employee.TerminationReason!
        );

        return Result<TerminateEmployeeResponse>.Ok(response, "Funcionário desligado com sucesso.");
    }
}
