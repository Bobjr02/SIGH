using SIGH.Domain.Common;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Employees.Helpers;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Employees.Entities;

public class Employee : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? SocialName { get; private set; }
    public string Cpf { get; private set; } = string.Empty;
    public DateOnly AdmissionDate { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public EmployeeStatus Status { get; private set; } = EmployeeStatus.PendingAdmission;
    public Guid JobTitleId { get; private set; }
    public Guid ManagementUnitId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? SupervisorId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? CorporateEmail { get; private set; }
    public string? PersonalEmail { get; private set; }
    public string? MobileNumber { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public string? TerminationReason { get; private set; }
    public string? Notes { get; private set; }

    public bool HasSystemAccess => UserId.HasValue;

    // EF Core
    public Employee() : base() { }

    public Employee(Guid id) : base(id) { }

    public static Employee Create(
        Guid companyId,
        string employeeNumber,
        string fullName,
        string cpf,
        DateOnly admissionDate,
        Guid jobTitleId,
        Guid managementUnitId,
        EmployeeStatus initialStatus = EmployeeStatus.PendingAdmission,
        string? socialName = null,
        DateOnly? birthDate = null,
        Guid? departmentId = null,
        Guid? supervisorId = null,
        Guid? userId = null,
        string? corporateEmail = null,
        string? personalEmail = null,
        string? mobileNumber = null,
        string? notes = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da empresa é obrigatório.");

        if (initialStatus != EmployeeStatus.PendingAdmission && initialStatus != EmployeeStatus.Active)
            throw new BusinessRuleValidationException("Status inicial inválido para o cadastro do funcionário.");

        var employee = new Employee
        {
            CompanyId = companyId,
            Status = initialStatus
        };

        employee.SetEmployeeNumber(employeeNumber);
        employee.SetFullName(fullName);
        employee.SetSocialName(socialName);
        employee.SetCpf(cpf);
        employee.SetAdmissionDate(admissionDate);
        employee.BirthDate = birthDate;
        employee.SetOrganizationalAssignment(jobTitleId, managementUnitId, departmentId);
        employee.ChangeSupervisor(supervisorId);

        if (userId.HasValue)
            employee.LinkUser(userId.Value);

        employee.SetEmails(corporateEmail, personalEmail);
        employee.SetMobileNumber(mobileNumber);
        employee.SetNotes(notes);

        return employee;
    }

    public void UpdatePersonalData(
        string fullName,
        string? socialName,
        DateOnly? birthDate,
        string? personalEmail,
        string? mobileNumber,
        string? notes = null)
    {
        SetFullName(fullName);
        SetSocialName(socialName);
        BirthDate = birthDate;
        SetPersonalEmail(personalEmail);
        SetMobileNumber(mobileNumber);
        SetNotes(notes);
    }

    public void UpdateCorporateData(
        string? corporateEmail,
        Guid jobTitleId,
        Guid managementUnitId,
        Guid? departmentId)
    {
        SetCorporateEmail(corporateEmail);
        SetOrganizationalAssignment(jobTitleId, managementUnitId, departmentId);
    }

    public void ChangeOrganizationalAssignment(Guid jobTitleId, Guid managementUnitId, Guid? departmentId)
    {
        SetOrganizationalAssignment(jobTitleId, managementUnitId, departmentId);
    }

    public void ChangeSupervisor(Guid? supervisorId)
    {
        if (supervisorId.HasValue && supervisorId.Value == Id && Id != Guid.Empty)
            throw new BusinessRuleValidationException("O funcionário não pode ser gestor de si mesmo.");

        SupervisorId = supervisorId;
    }

    public void LinkUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do usuário é obrigatório para vínculo.");

        if (UserId.HasValue && UserId.Value == userId)
            return; // Idempotent

        if (UserId.HasValue && UserId.Value != userId)
            throw new BusinessRuleValidationException("Para vincular outro usuário, é necessário desvincular o usuário atual primeiro.");

        UserId = userId;
    }

    public void UnlinkUser()
    {
        UserId = null;
    }

    public void UpdateEmployeeNumber(string employeeNumber)
    {
        SetEmployeeNumber(employeeNumber);
    }

    public void UpdateCpf(string cpf)
    {
        SetCpf(cpf);
    }

    public void Activate()
    {
        if (Status == EmployeeStatus.Active)
            return; // Idempotent

        if (Status == EmployeeStatus.Terminated)
            throw new BusinessRuleValidationException("Para reativar um funcionário desligado, utilize o método Reativar.");

        Status = EmployeeStatus.Active;
    }

    public void SetOnLeave()
    {
        if (Status == EmployeeStatus.OnLeave)
            return; // Idempotent

        if (Status != EmployeeStatus.Active)
            throw new BusinessRuleValidationException("Apenas funcionários com status Ativo podem ser colocados em afastamento.");

        Status = EmployeeStatus.OnLeave;
    }

    public void SetInactive()
    {
        if (Status == EmployeeStatus.Inactive)
            return; // Idempotent

        if (Status == EmployeeStatus.Terminated)
            throw new BusinessRuleValidationException("Funcionário desligado não pode ser alterado para Inativo.");

        Status = EmployeeStatus.Inactive;
    }

    public void Terminate(DateOnly terminationDate, string terminationReason)
    {
        if (Status == EmployeeStatus.Terminated)
            throw new BusinessRuleValidationException("Funcionário já se encontra desligado.");

        if (Status == EmployeeStatus.PendingAdmission)
            throw new BusinessRuleValidationException("Funcionário pendente de admissão não pode ser desligado diretamente.");

        if (terminationDate < AdmissionDate)
            throw new BusinessRuleValidationException("Data de desligamento não pode ser anterior à data de admissão.");

        if (string.IsNullOrWhiteSpace(terminationReason))
            throw new BusinessRuleValidationException("O motivo do desligamento é obrigatório.");

        var trimmedReason = terminationReason.Trim();
        if (trimmedReason.Length > 500)
            throw new BusinessRuleValidationException("O motivo do desligamento deve ter no máximo 500 caracteres.");

        Status = EmployeeStatus.Terminated;
        TerminationDate = terminationDate;
        TerminationReason = trimmedReason;
    }

    public void Reactivate(DateOnly newAdmissionDate)
    {
        if (Status != EmployeeStatus.Terminated)
            throw new BusinessRuleValidationException("Apenas funcionários desligados podem ser reativados.");

        if (newAdmissionDate == DateOnly.MinValue)
            throw new BusinessRuleValidationException("Nova data de admissão inválida.");

        if (newAdmissionDate < AdmissionDate)
            throw new BusinessRuleValidationException("A nova data de admissão não pode ser anterior à admissão anterior.");

        Status = EmployeeStatus.Active;
        AdmissionDate = newAdmissionDate;
        TerminationDate = null;
        TerminationReason = null;
    }

    private void SetEmployeeNumber(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new BusinessRuleValidationException("A matrícula do funcionário é obrigatória.");

        var trimmed = employeeNumber.Trim();
        if (trimmed.Length > 20)
            throw new BusinessRuleValidationException("A matrícula deve ter no máximo 20 caracteres.");

        EmployeeNumber = trimmed;
    }

    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new BusinessRuleValidationException("O nome completo do funcionário é obrigatório.");

        var trimmed = fullName.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome completo deve ter no máximo 150 caracteres.");

        FullName = trimmed;
    }

    private void SetSocialName(string? socialName)
    {
        if (string.IsNullOrWhiteSpace(socialName))
        {
            SocialName = null;
            return;
        }

        var trimmed = socialName.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome social deve ter no máximo 150 caracteres.");

        SocialName = trimmed;
    }

    private void SetCpf(string rawCpf)
    {
        if (string.IsNullOrWhiteSpace(rawCpf))
            throw new BusinessRuleValidationException("O CPF do funcionário é obrigatório.");

        if (!CpfValidator.IsValid(rawCpf))
            throw new BusinessRuleValidationException("CPF inválido.");

        Cpf = CpfValidator.Normalize(rawCpf);
    }

    private void SetAdmissionDate(DateOnly admissionDate)
    {
        if (admissionDate == DateOnly.MinValue)
            throw new BusinessRuleValidationException("A data de admissão é obrigatória.");

        AdmissionDate = admissionDate;
    }

    private void SetOrganizationalAssignment(Guid jobTitleId, Guid managementUnitId, Guid? departmentId)
    {
        if (jobTitleId == Guid.Empty)
            throw new BusinessRuleValidationException("O cargo é obrigatório.");

        if (managementUnitId == Guid.Empty)
            throw new BusinessRuleValidationException("A unidade gestora é obrigatória.");

        if (departmentId.HasValue && departmentId.Value == Guid.Empty)
            throw new BusinessRuleValidationException("ID do departamento inválido.");

        JobTitleId = jobTitleId;
        ManagementUnitId = managementUnitId;
        DepartmentId = departmentId;
    }

    private void SetEmails(string? corporateEmail, string? personalEmail)
    {
        SetCorporateEmail(corporateEmail);
        SetPersonalEmail(personalEmail);
    }

    private void SetCorporateEmail(string? corporateEmail)
    {
        if (string.IsNullOrWhiteSpace(corporateEmail))
        {
            CorporateEmail = null;
            return;
        }

        var trimmed = corporateEmail.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O e-mail corporativo deve ter no máximo 150 caracteres.");

        if (!IsValidEmail(trimmed))
            throw new BusinessRuleValidationException("E-mail corporativo em formato inválido.");

        CorporateEmail = trimmed;
    }

    private void SetPersonalEmail(string? personalEmail)
    {
        if (string.IsNullOrWhiteSpace(personalEmail))
        {
            PersonalEmail = null;
            return;
        }

        var trimmed = personalEmail.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O e-mail pessoal deve ter no máximo 150 caracteres.");

        if (!IsValidEmail(trimmed))
            throw new BusinessRuleValidationException("E-mail pessoal em formato inválido.");

        PersonalEmail = trimmed;
    }

    private void SetMobileNumber(string? mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            MobileNumber = null;
            return;
        }

        var trimmed = mobileNumber.Trim();
        if (trimmed.Length > 20)
            throw new BusinessRuleValidationException("O número de celular deve ter no máximo 20 caracteres.");

        MobileNumber = trimmed;
    }

    private void SetNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            Notes = null;
            return;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > 1000)
            throw new BusinessRuleValidationException("As observações devem ter no máximo 1000 caracteres.");

        Notes = trimmed;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            return false;

        return parts[1].Contains('.');
    }
}
