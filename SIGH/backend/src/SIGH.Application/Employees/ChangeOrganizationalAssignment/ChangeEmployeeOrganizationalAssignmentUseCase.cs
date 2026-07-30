using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.ChangeOrganizationalAssignment;

public class ChangeEmployeeOrganizationalAssignmentUseCase : IChangeEmployeeOrganizationalAssignmentUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IManagementUnitRepository _managementUnitRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public ChangeEmployeeOrganizationalAssignmentUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        IJobTitleRepository jobTitleRepository,
        IManagementUnitRepository managementUnitRepository,
        IDepartmentRepository departmentRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
        _jobTitleRepository = jobTitleRepository;
        _managementUnitRepository = managementUnitRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> ExecuteAsync(ChangeEmployeeOrganizationalAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result.FailureResult("Funcionário não encontrado.", EmployeeErrors.NotFound);

        // Cargo
        var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle == null)
            return Result.FailureResult("Cargo não encontrado.", EmployeeErrors.JobTitleNotFound);

        if (!jobTitle.IsActive)
            return Result.FailureResult("O cargo informado está inativo.", EmployeeErrors.InactiveJobTitle);

        if (jobTitle.CompanyId != employee.CompanyId)
            return Result.FailureResult("O cargo não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

        // Unidade Gestora
        var unit = await _managementUnitRepository.GetByIdAsync(request.ManagementUnitId, cancellationToken);
        if (unit == null)
            return Result.FailureResult("Unidade gestora não encontrada.", EmployeeErrors.ManagementUnitNotFound);

        if (!unit.IsActive)
            return Result.FailureResult("A unidade gestora informada está inativa.", EmployeeErrors.InactiveManagementUnit);

        if (unit.CompanyId != employee.CompanyId)
            return Result.FailureResult("A unidade gestora não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

        // Departamento (opcional)
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            var dept = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
            if (dept == null)
                return Result.FailureResult("Departamento não encontrado.", EmployeeErrors.DepartmentNotFound);

            if (!dept.IsActive)
                return Result.FailureResult("O departamento informado está inativo.", EmployeeErrors.InactiveDepartment);

            if (dept.CompanyId != employee.CompanyId)
                return Result.FailureResult("O departamento não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

            if (dept.ManagementUnitId != request.ManagementUnitId)
                return Result.FailureResult("O departamento não pertence à unidade gestora informada.", EmployeeErrors.InvalidData);
        }

        try
        {
            employee.ChangeOrganizationalAssignment(request.JobTitleId, request.ManagementUnitId, request.DepartmentId);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.FailureResult(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.SuccessResult("Lotação organizacional alterada com sucesso.");
    }
}
