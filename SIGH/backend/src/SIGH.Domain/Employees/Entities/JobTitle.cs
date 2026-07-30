using SIGH.Domain.Common;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Employees.Entities;

public class JobTitle : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core
    public JobTitle() : base() { }

    public JobTitle(Guid id) : base(id) { }

    public static JobTitle Create(Guid companyId, string name, string? code = null, string? description = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da empresa é obrigatório.");

        var jobTitle = new JobTitle
        {
            CompanyId = companyId,
            IsActive = true
        };

        jobTitle.SetName(name);
        jobTitle.SetCode(code);
        jobTitle.SetDescription(description);

        return jobTitle;
    }

    public void UpdateName(string name)
    {
        SetName(name);
    }

    public void UpdateCode(string? code)
    {
        SetCode(code);
    }

    public void UpdateDescription(string? description)
    {
        SetDescription(description);
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
            throw new BusinessRuleValidationException("O nome do cargo é obrigatório.");

        var trimmed = name.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome do cargo deve ter no máximo 150 caracteres.");

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
            throw new BusinessRuleValidationException("O código do cargo deve ter no máximo 30 caracteres.");

        Code = trimmed;
    }

    private void SetDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 500)
            throw new BusinessRuleValidationException("A descrição do cargo deve ter no máximo 500 caracteres.");

        Description = trimmed;
    }
}
