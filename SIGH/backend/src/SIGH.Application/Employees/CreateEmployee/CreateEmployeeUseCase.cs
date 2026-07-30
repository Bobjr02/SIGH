using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.CreateEmployee;

public class CreateEmployeeUseCase : ICreateEmployeeUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IManagementUnitRepository _managementUnitRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IUserRepository _userRepository;

    public CreateEmployeeUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository,
        IManagementUnitRepository managementUnitRepository,
        IDepartmentRepository departmentRepository,
        IJobTitleRepository jobTitleRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _managementUnitRepository = managementUnitRepository;
        _departmentRepository = departmentRepository;
        _jobTitleRepository = jobTitleRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CreateEmployeeResponse>> ExecuteAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validar Empresa
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company == null)
            return Result<CreateEmployeeResponse>.Failure("Empresa não encontrada.", EmployeeErrors.CompanyNotFound);

        if (!company.IsActive)
            return Result<CreateEmployeeResponse>.Failure("A empresa informada está inativa.", EmployeeErrors.InactiveCompany);

        // 2. Validar Cargo
        var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle == null)
            return Result<CreateEmployeeResponse>.Failure("Cargo não encontrado.", EmployeeErrors.JobTitleNotFound);

        if (!jobTitle.IsActive)
            return Result<CreateEmployeeResponse>.Failure("O cargo informado está inativo.", EmployeeErrors.InactiveJobTitle);

        if (jobTitle.CompanyId != request.CompanyId)
            return Result<CreateEmployeeResponse>.Failure("O cargo não pertence à empresa informada.", EmployeeErrors.InvalidData);

        // 3. Validar Unidade Gestora
        var managementUnit = await _managementUnitRepository.GetByIdAsync(request.ManagementUnitId, cancellationToken);
        if (managementUnit == null)
            return Result<CreateEmployeeResponse>.Failure("Unidade gestora não encontrada.", EmployeeErrors.ManagementUnitNotFound);

        if (!managementUnit.IsActive)
            return Result<CreateEmployeeResponse>.Failure("A unidade gestora informada está inativa.", EmployeeErrors.InactiveManagementUnit);

        if (managementUnit.CompanyId != request.CompanyId)
            return Result<CreateEmployeeResponse>.Failure("A unidade gestora não pertence à empresa informada.", EmployeeErrors.InvalidData);

        // 4. Validar Departamento (opcional)
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
            if (department == null)
                return Result<CreateEmployeeResponse>.Failure("Departamento não encontrado.", EmployeeErrors.DepartmentNotFound);

            if (!department.IsActive)
                return Result<CreateEmployeeResponse>.Failure("O departamento informado está inativo.", EmployeeErrors.InactiveDepartment);

            if (department.CompanyId != request.CompanyId)
                return Result<CreateEmployeeResponse>.Failure("O departamento não pertence à empresa informada.", EmployeeErrors.InvalidData);

            if (department.ManagementUnitId != request.ManagementUnitId)
                return Result<CreateEmployeeResponse>.Failure("O departamento não pertence à unidade gestora informada.", EmployeeErrors.InvalidData);
        }

        // 5. Validar Gestor (opcional)
        if (request.SupervisorId.HasValue && request.SupervisorId.Value != Guid.Empty)
        {
            var supervisor = await _employeeRepository.GetByIdAsync(request.SupervisorId.Value, cancellationToken);
            if (supervisor == null)
                return Result<CreateEmployeeResponse>.Failure("Gestor não encontrado.", EmployeeErrors.SupervisorNotFound);

            if (supervisor.CompanyId != request.CompanyId)
                return Result<CreateEmployeeResponse>.Failure("O gestor deve pertencer à mesma empresa do funcionário.", EmployeeErrors.SupervisorFromAnotherCompany);
        }

        // 6. Validar Usuário (opcional)
        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            var userExists = await _userRepository.ExistsAsync(request.UserId.Value, cancellationToken);
            if (!userExists)
                return Result<CreateEmployeeResponse>.Failure("Usuário não encontrado.", EmployeeErrors.UserNotFound);

            var isUserLinked = await _employeeRepository.IsUserLinkedAsync(request.UserId.Value, cancellationToken: cancellationToken);
            if (isUserLinked)
                return Result<CreateEmployeeResponse>.Failure("O usuário informado já está vinculado a outro funcionário.", EmployeeErrors.UserAlreadyLinked);
        }

        // 7. Validar Unicidade de CPF
        var cpfExists = await _employeeRepository.ExistsByCpfAsync(request.Cpf, cancellationToken: cancellationToken);
        if (cpfExists)
            return Result<CreateEmployeeResponse>.Failure("Já existe um funcionário cadastrado com este CPF.", EmployeeErrors.CpfAlreadyExists);

        // 8. Validar Unicidade de Matrícula na Empresa
        var numberExists = await _employeeRepository.ExistsByEmployeeNumberAsync(request.CompanyId, request.EmployeeNumber, cancellationToken: cancellationToken);
        if (numberExists)
            return Result<CreateEmployeeResponse>.Failure("Já existe um funcionário cadastrado com esta matrícula na empresa.", EmployeeErrors.EmployeeNumberAlreadyExists);

        // 9. Validar Unicidade de E-mail Corporativo (se informado)
        if (!string.IsNullOrWhiteSpace(request.CorporateEmail))
        {
            var emailExists = await _employeeRepository.ExistsByCorporateEmailAsync(request.CorporateEmail, cancellationToken: cancellationToken);
            if (emailExists)
                return Result<CreateEmployeeResponse>.Failure("Já existe um funcionário cadastrado com este e-mail corporativo.", EmployeeErrors.CorporateEmailAlreadyExists);
        }

        // 10. Criar entidade de domínio
        Employee employee;
        try
        {
            employee = Employee.Create(
                companyId: request.CompanyId,
                employeeNumber: request.EmployeeNumber,
                fullName: request.FullName,
                cpf: request.Cpf,
                admissionDate: request.AdmissionDate,
                jobTitleId: request.JobTitleId,
                managementUnitId: request.ManagementUnitId,
                initialStatus: request.InitialStatus,
                socialName: request.SocialName,
                birthDate: request.BirthDate,
                departmentId: request.DepartmentId,
                supervisorId: request.SupervisorId,
                userId: request.UserId,
                corporateEmail: request.CorporateEmail,
                personalEmail: request.PersonalEmail,
                mobileNumber: request.MobileNumber,
                notes: request.Notes
            );
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<CreateEmployeeResponse>.Failure(ex.Message, EmployeeErrors.InvalidData);
        }

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateEmployeeResponse(
            employee.Id,
            employee.CompanyId,
            employee.EmployeeNumber,
            employee.FullName,
            employee.SocialName,
            CpfMasker.Mask(employee.Cpf),
            employee.AdmissionDate,
            employee.Status,
            employee.JobTitleId,
            employee.ManagementUnitId,
            employee.DepartmentId,
            employee.SupervisorId,
            employee.UserId,
            employee.HasSystemAccess
        );

        return Result<CreateEmployeeResponse>.Ok(response, "Funcionário cadastrado com sucesso.");
    }
}
