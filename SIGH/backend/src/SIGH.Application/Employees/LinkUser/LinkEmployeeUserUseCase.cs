using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.LinkUser;

public class LinkEmployeeUserUseCase : ILinkEmployeeUserUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public LinkEmployeeUserUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
    }

    public async Task<Result> ExecuteAsync(LinkEmployeeUserRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        var userExists = await _userRepository.ExistsAsync(request.UserId, cancellationToken);
        if (!userExists)
            return Result.FailureResult("Usuário não encontrado.", EmployeeErrors.UserNotFound);

        var isUserLinked = await _employeeRepository.IsUserLinkedAsync(request.UserId, excludingEmployeeId: employee.Id, cancellationToken: cancellationToken);
        if (isUserLinked)
            return Result.FailureResult("O usuário informado já está vinculado a outro funcionário.", EmployeeErrors.UserAlreadyLinked);

        try
        {
            employee.LinkUser(request.UserId);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.FailureResult(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Usuário vinculado ao funcionário com sucesso.");
    }
}
