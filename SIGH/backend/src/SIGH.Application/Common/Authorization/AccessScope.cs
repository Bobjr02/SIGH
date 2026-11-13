using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Entities;

namespace SIGH.Application.Common.Authorization;

/// <summary>
/// Escopo de dados resolvido para o usuário da requisição: tudo (permissão Disciplinary.Records.ViewAll)
/// ou só os funcionários cuja gerência ATUAL está entre as gerências vigentes do usuário.
/// </summary>
public sealed record AccessScopeFilter(bool All, IReadOnlyCollection<Guid> ManagementUnitIds)
{
    public static readonly AccessScopeFilter Everything = new(true, Array.Empty<Guid>());
    public static readonly AccessScopeFilter Nothing = new(false, Array.Empty<Guid>());

    public bool Allows(Guid managementUnitId) => All || ManagementUnitIds.Contains(managementUnitId);
}

/// <summary>
/// Escopo por gerência, aplicado no SERVIDOR em toda leitura de dados de funcionários e de medidas.
/// Fora do escopo: listas não trazem o registro e o detalhe responde 404 (nunca 403).
/// </summary>
public interface IAccessScope
{
    /// <summary>Permissão de escopo total (Disciplinary.Records.ViewAll).</summary>
    bool HasFullAccess { get; }

    /// <summary>Gerências vigentes agora (ValidFrom/ValidUntil) do usuário da requisição.</summary>
    Task<IReadOnlyCollection<Guid>> GetManagementUnitIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Filtro pronto (resolvido uma vez por requisição).</summary>
    Task<AccessScopeFilter> GetFilterAsync(CancellationToken cancellationToken = default);
}

/// <summary>Filtros LINQ do escopo, traduzidos para SQL pelo EF (paginação continua no banco).</summary>
public static class AccessScopeQueryExtensions
{
    public static IQueryable<Employee> InScope(this IQueryable<Employee> employees, AccessScopeFilter scope)
    {
        if (scope.All)
            return employees;
        var ids = scope.ManagementUnitIds.ToList();
        return employees.Where(e => ids.Contains(e.ManagementUnitId));
    }

    /// <summary>
    /// Processo/medida visível se o funcionário ACUSADO pertence HOJE a uma gerência do escopo:
    /// o histórico acompanha o funcionário quando ele muda de gerência.
    /// </summary>
    public static IQueryable<DisciplinaryCase> InScope(
        this IQueryable<DisciplinaryCase> cases,
        IQueryable<DisciplinaryCaseEmployee> caseEmployees,
        IQueryable<Employee> employees,
        AccessScopeFilter scope)
    {
        if (scope.All)
            return cases;
        var ids = scope.ManagementUnitIds.ToList();
        return cases.Where(c => caseEmployees.Any(ce => ce.DisciplinaryCaseId == c.Id
                                                        && ce.Role == CaseEmployeeRole.Accused
                                                        && employees.Any(e => e.Id == ce.EmployeeId && ids.Contains(e.ManagementUnitId))));
    }

    /// <summary>Ids dos processos visíveis (para filtrar tabelas que só têm o DisciplinaryCaseId).</summary>
    public static IQueryable<Guid> VisibleCaseIds(
        IQueryable<DisciplinaryCaseEmployee> caseEmployees,
        IQueryable<Employee> employees,
        AccessScopeFilter scope)
    {
        var ids = scope.ManagementUnitIds.ToList();
        return caseEmployees
            .Where(ce => ce.Role == CaseEmployeeRole.Accused
                         && employees.Any(e => e.Id == ce.EmployeeId && ids.Contains(e.ManagementUnitId)))
            .Select(ce => ce.DisciplinaryCaseId);
    }
}
