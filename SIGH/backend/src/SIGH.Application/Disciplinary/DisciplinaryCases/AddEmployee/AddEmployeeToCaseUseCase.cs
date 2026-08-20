using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;

public class AddEmployeeToCaseUseCase : IAddEmployeeToCaseUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public AddEmployeeToCaseUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _caseRepository = caseRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<AddEmployeeToCaseResponse>> ExecuteAsync(AddEmployeeToCaseRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<AddEmployeeToCaseResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<AddEmployeeToCaseResponse>.Failure("Funcionário não encontrado.", DisciplinaryErrors.EmployeeNotFound);
        }

        var caseEmployee = DisciplinaryCaseEmployee.Create(
            disciplinaryCaseId: caseObj.Id,
            employeeId: employee.Id,
            role: request.Role,
            isPrimarySubject: request.IsPrimarySubject);

        caseObj.AddEmployee(caseEmployee);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AddEmployeeToCaseResponse(
            caseEmployee.Id,
            caseObj.Id,
            employee.Id,
            caseEmployee.Role,
            caseEmployee.IsPrimarySubject);

        return Result<AddEmployeeToCaseResponse>.Ok(response, "Funcionário vinculado ao processo disciplinar com sucesso.");
    }
}
