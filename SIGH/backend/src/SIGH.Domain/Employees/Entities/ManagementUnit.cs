using SIGH.Domain.Common;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Employees.Entities;

public class ManagementUnit : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public Guid? ParentManagementUnitId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core
    public ManagementUnit() : base() { }

    public ManagementUnit(Guid id) : base(id) { }

    public static ManagementUnit Create(Guid companyId, string name, string? code = null, Guid? parentManagementUnitId = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da empresa é obrigatório.");

        var unit = new ManagementUnit
        {
            CompanyId = companyId,
            IsActive = true
        };

        unit.SetName(name);
        unit.SetCode(code);
        unit.ChangeParent(parentManagementUnitId);

        return unit;
    }

    public void UpdateName(string name)
    {
        SetName(name);
    }

    public void UpdateCode(string? code)
    {
        SetCode(code);
    }

    public void ChangeParent(Guid? parentManagementUnitId)
    {
        if (parentManagementUnitId.HasValue && parentManagementUnitId.Value == Id && Id != Guid.Empty)
            throw new BusinessRuleValidationException("Uma unidade gestora não pode ser pai de si mesma.");

        ParentManagementUnitId = parentManagementUnitId;
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
            throw new BusinessRuleValidationException("O nome da unidade gestora é obrigatório.");

        var trimmed = name.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome da unidade gestora deve ter no máximo 150 caracteres.");

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
            throw new BusinessRuleValidationException("O código da unidade gestora deve ter no máximo 30 caracteres.");

        Code = trimmed;
    }
}
