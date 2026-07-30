using System.Text.RegularExpressions;
using SIGH.Domain.Common;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Employees.Entities;

public class Company : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core
    public Company() : base() { }

    public Company(Guid id) : base(id) { }

    public static Company Create(string name, string? legalName = null, string? registrationNumber = null)
    {
        var company = new Company();
        company.SetName(name);
        company.SetLegalName(legalName);
        company.SetRegistrationNumber(registrationNumber);
        company.IsActive = true;
        return company;
    }

    public void UpdateName(string name)
    {
        SetName(name);
    }

    public void UpdateLegalName(string? legalName)
    {
        SetLegalName(legalName);
    }

    public void UpdateRegistrationNumber(string? registrationNumber)
    {
        SetRegistrationNumber(registrationNumber);
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
            throw new BusinessRuleValidationException("O nome da empresa é obrigatório.");

        var trimmed = name.Trim();
        if (trimmed.Length > 150)
            throw new BusinessRuleValidationException("O nome da empresa deve ter no máximo 150 caracteres.");

        Name = trimmed;
    }

    private void SetLegalName(string? legalName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            LegalName = null;
            return;
        }

        var trimmed = legalName.Trim();
        if (trimmed.Length > 200)
            throw new BusinessRuleValidationException("Razão Social deve ter no máximo 200 caracteres.");

        LegalName = trimmed;
    }

    private void SetRegistrationNumber(string? registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            RegistrationNumber = null;
            return;
        }

        // Normalize: remove punctuation and whitespace
        var normalized = Regex.Replace(registrationNumber, @"[^\w]", string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            RegistrationNumber = null;
            return;
        }

        if (normalized.Length > 20)
            throw new BusinessRuleValidationException("O número de registro da empresa deve ter no máximo 20 caracteres.");

        RegistrationNumber = normalized;
    }
}
