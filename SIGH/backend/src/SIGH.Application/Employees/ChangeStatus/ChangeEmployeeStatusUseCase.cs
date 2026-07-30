using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.ChangeStatus;

public class ChangeEmployeeStatusUseCase : IChangeEmployeeStatusUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;

    public ChangeEmployeeStatusUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> ExecuteAsync(ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        try
        {
            switch (request.TargetStatus)
            {
                case EmployeeStatus.Active:
                    employee.Activate();
                    break;
                case EmployeeStatus.OnLeave:
                    employee.SetOnLeave();
                    break;
                case EmployeeStatus.Inactive:
                    employee.SetInactive();
                    break;
                default:
                    return Result.FailureResult("Transição de status inválida. Para desligar ou reativar, utilize o caso de uso específico.", EmployeeErrors.InvalidStatusTransition);
            }
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.FailureResult(ex.Message, EmployeeErrors.InvalidStatusTransition);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Status do funcionário alterado com sucesso.");
    }
}
