using SIGH.Domain.Employees.Enums;

namespace SIGH.Domain.Employees.Entities;

/// <summary>Dados de um funcionário vindos do sistema de RH (SAP), já validados e resolvidos para ids.</summary>
public sealed record HrRecord(
    string FullName,
    string Cpf,
    DateOnly AdmissionDate,
    Guid JobTitleId,
    Guid ManagementUnitId,
    Guid? DepartmentId,
    EmployeeGroup? Group,
    string? Team,
    EmployeeStatus Status,
    DateOnly? TerminationDate,
    string? TerminationReason = null);
