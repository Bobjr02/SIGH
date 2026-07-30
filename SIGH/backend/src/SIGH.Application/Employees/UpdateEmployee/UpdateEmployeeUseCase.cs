using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.UpdateEmployee;

public class UpdateEmployeeUseCase : IUpdateEmployeeUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IManagementUnitRepository _managementUnitRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IJobTitleRepository _jobTitleRepository;

    public UpdateEmployeeUseCase(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository,
        IManagementUnitRepository managementUnitRepository,
        IDepartmentRepository departmentRepository,
        IJobTitleRepository jobTitleRepository)
    {
        _context = context;
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _managementUnitRepository = managementUnitRepository;
        _departmentRepository = departmentRepository;
        _jobTitleRepository = jobTitleRepository;
    }

    public async Task<Result<UpdateEmployeeResponse>> ExecuteAsync(UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
            return Result<UpdateEmployeeResponse>.Failure("Funcionário não encontrado.", EmployeeErrors.NotFound);

        // 1. Validar Empresa
        var company = await _companyRepository.GetByIdAsync(employee.CompanyId, cancellationToken);
        if (company == null)
            return Result<UpdateEmployeeResponse>.Failure("Empresa não encontrada.", EmployeeErrors.CompanyNotFound);

        if (!company.IsActive)
            return Result<UpdateEmployeeResponse>.Failure("A empresa informada está inativa.", EmployeeErrors.InactiveCompany);

        // 2. Validar Cargo
        var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle == null)
            return Result<UpdateEmployeeResponse>.Failure("Cargo não encontrado.", EmployeeErrors.JobTitleNotFound);

        if (!jobTitle.IsActive)
            return Result<UpdateEmployeeResponse>.Failure("O cargo informado está inativo.", EmployeeErrors.InactiveJobTitle);

        if (jobTitle.CompanyId != employee.CompanyId)
            return Result<UpdateEmployeeResponse>.Failure("O cargo não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

        // 3. Validar Unidade Gestora
        var managementUnit = await _managementUnitRepository.GetByIdAsync(request.ManagementUnitId, cancellationToken);
        if (managementUnit == null)
            return Result<UpdateEmployeeResponse>.Failure("Unidade gestora não encontrada.", EmployeeErrors.ManagementUnitNotFound);

        if (!managementUnit.IsActive)
            return Result<UpdateEmployeeResponse>.Failure("A unidade gestora informada está inativa.", EmployeeErrors.InactiveManagementUnit);

        if (managementUnit.CompanyId != employee.CompanyId)
            return Result<UpdateEmployeeResponse>.Failure("A unidade gestora não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

        // 4. Validar Departamento (opcional)
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
            if (department == null)
                return Result<UpdateEmployeeResponse>.Failure("Departamento não encontrado.", EmployeeErrors.DepartmentNotFound);

            if (!department.IsActive)
                return Result<UpdateEmployeeResponse>.Failure("O departamento informado está inativo.", EmployeeErrors.InactiveDepartment);

            if (department.CompanyId != employee.CompanyId)
                return Result<UpdateEmployeeResponse>.Failure("O departamento não pertence à empresa do funcionário.", EmployeeErrors.InvalidData);

            if (department.ManagementUnitId != request.ManagementUnitId)
                return Result<UpdateEmployeeResponse>.Failure("O departamento não pertence à unidade gestora informada.", EmployeeErrors.InvalidData);
        }

        // 5. Validar Unicidades
        var cpfExists = await _employeeRepository.ExistsByCpfAsync(request.Cpf, excludingEmployeeId: employee.Id, cancellationToken: cancellationToken);
        if (cpfExists)
            return Result<UpdateEmployeeResponse>.Failure("Já existe outro funcionário cadastrado com este CPF.", EmployeeErrors.CpfAlreadyExists);

        var numberExists = await _employeeRepository.ExistsByEmployeeNumberAsync(employee.CompanyId, request.EmployeeNumber, excludingEmployeeId: employee.Id, cancellationToken: cancellationToken);
        if (numberExists)
            return Result<UpdateEmployeeResponse>.Failure("Já existe outro funcionário cadastrado com esta matrícula na mesma empresa.", EmployeeErrors.EmployeeNumberAlreadyExists);

        if (!string.IsNullOrWhiteSpace(request.CorporateEmail))
        {
            var emailExists = await _employeeRepository.ExistsByCorporateEmailAsync(request.CorporateEmail, excludingEmployeeId: employee.Id, cancellationToken: cancellationToken);
            if (emailExists)
                return Result<UpdateEmployeeResponse>.Failure("Já existe outro funcionário cadastrado com este e-mail corporativo.", EmployeeErrors.CorporateEmailAlreadyExists);
        }

        // 6. Atualizar Entidade
        try
        {
            employee.UpdateEmployeeNumber(request.EmployeeNumber);
            employee.UpdateCpf(request.Cpf);
            employee.UpdatePersonalData(
                fullName: request.FullName,
                socialName: request.SocialName,
                birthDate: request.BirthDate,
                personalEmail: request.PersonalEmail,
                mobileNumber: request.MobileNumber,
                notes: request.Notes
            );
            employee.UpdateCorporateData(
                corporateEmail: request.CorporateEmail,
                jobTitleId: request.JobTitleId,
                managementUnitId: request.ManagementUnitId,
                departmentId: request.DepartmentId
            );
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<UpdateEmployeeResponse>.Failure(ex.Message, EmployeeErrors.InvalidData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new UpdateEmployeeResponse(
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
            employee.CorporateEmail,
            employee.PersonalEmail,
            employee.MobileNumber,
            employee.Notes
        );

        return Result<UpdateEmployeeResponse>.Ok(response, "Dados do funcionário atualizados com sucesso.");
    }
}
