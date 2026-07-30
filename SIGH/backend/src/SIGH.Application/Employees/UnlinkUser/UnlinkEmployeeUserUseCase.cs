using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.UnlinkUser;

public class UnlinkEmployeeUserUseCase : IUnlinkEmployeeUserUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;

    public UnlinkEmployeeUserUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> ExecuteAsync(UnlinkEmployeeUserRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        employee.UnlinkUser();

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Usuário desvinculado do funcionário com sucesso.");
    }
}
