using SIGH.Domain.Common;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Employees.Entities;

public class Department : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid ManagementUnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core
    public Department() : base() { }

    public Department(Guid id) : base(id) { }

    public static Department Create(Guid companyId, Guid managementUnitId, string name, string? code = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da empresa é obrigatório.");

        var dept = new Department
        {
            CompanyId = companyId,
            IsActive = true
        };

        dept.ChangeManagementUnit(managementUnitId);
        dept.SetName(name);
        dept.SetCode(code);

        return dept;
    }

    public void UpdateName(string name)
    {
        SetName(name);
    }

    public void UpdateCode(string? code)
    {
        SetCode(code);
    }

    public void ChangeManagementUnit(Guid managementUnitId)
    {
        if (managementUnitId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da unidade gestora é obrigatório.");

        ManagementUnitId = managementUnitId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("O nome do departamento é obrigatório.");

        var trimmed = name.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome do departamento deve ter no máximo 150 caracteres.");

        Name = trimmed;
    }

    private void SetCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Code = null;
            return;
        }

        var trimmed = code.Trim();
        if (trimmed.Length > 30)
            throw new BusinessRuleValidationException("O código do departamento deve ter no máximo 30 caracteres.");

        Code = trimmed;
    }
}
