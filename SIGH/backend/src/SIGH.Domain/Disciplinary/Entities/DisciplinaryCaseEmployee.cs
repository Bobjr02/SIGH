using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryCaseEmployee : AuditableEntity
{
    public Guid DisciplinaryCaseId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public CaseEmployeeRole Role { get; private set; }
    public string? Statement { get; private set; }
    public DateTimeOffset? StatementRecordedAt { get; private set; }
    public bool IsPrimarySubject { get; private set; }

    // EF Core
    protected DisciplinaryCaseEmployee() : base() { }

    protected DisciplinaryCaseEmployee(Guid id) : base(id) { }

    public static DisciplinaryCaseEmployee Create(
        Guid disciplinaryCaseId,
        Guid employeeId,
        CaseEmployeeRole role,
        bool isPrimarySubject = false)
    {
        if (disciplinaryCaseId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do processo disciplinar é obrigatório.");

        if (employeeId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do funcionário é obrigatório.");

        if (role == CaseEmployeeRole.Undefined || !Enum.IsDefined(role))
            throw new BusinessRuleValidationException("O papel do funcionário no processo é inválido.");

        return new DisciplinaryCaseEmployee
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            EmployeeId = employeeId,
            Role = role,
            IsPrimarySubject = isPrimarySubject
        };
    }

    internal void RegisterStatement(string statement, DateTimeOffset recordedAt)
    {
        if (recordedAt == default)
            throw new BusinessRuleValidationException("A data do depoimento é inválida.");

        if (string.IsNullOrWhiteSpace(statement))
            throw new BusinessRuleValidationException("O depoimento não pode ser vazio.");

        var trimmedStatement = statement.Trim();
        if (trimmedStatement.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"O depoimento não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        Statement = trimmedStatement;
        StatementRecordedAt = recordedAt;
    }

    internal void UpdateRole(CaseEmployeeRole newRole)
    {
        if (newRole == CaseEmployeeRole.Undefined || !Enum.IsDefined(newRole))
            throw new BusinessRuleValidationException("O papel do funcionário no processo é inválido.");

        Role = newRole;
    }

    internal void MarkAsPrimarySubject()
    {
        IsPrimarySubject = true;
    }

    internal void UnmarkAsPrimarySubject()
    {
        IsPrimarySubject = false;
    }
}
