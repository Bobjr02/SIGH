namespace SIGH.IntegrationTests.Scope;

/// <summary>
/// Classificação EXPLÍCITA de toda rota GET da API quanto ao escopo por gerência (Parte C).
/// Uma rota nova sem classificação faz <see cref="RouteScopeCoverageTests"/> falhar: decida se ela lê
/// dados de funcionários/medidas (com escopo, e então precisa de um caso em <see cref="ScopeEnforcementTests"/>)
/// ou não (não se aplica, com o motivo).
/// </summary>
public static class RouteScopeClassification
{
    public static readonly IReadOnlySet<string> Scoped = new HashSet<string>(StringComparer.Ordinal)
    {
        // Funcionários
        "/api/v1/employees",
        "/api/v1/employees/{id:guid}",
        "/api/v1/employees/{id:guid}/decision-support",
        "/api/v1/employees/filter-options",
        // Medidas
        "/api/v1/disciplinary-records",
        "/api/v1/disciplinary-records/{id:guid}",
        "/api/v1/disciplinary-records/options",
        "/api/v1/disciplinary-records/pending-count",
        // Processos (legado, fora do menu) e trilha de auditoria de um processo
        "/api/v1/disciplinary-cases",
        "/api/v1/disciplinary-cases/{id:guid}",
        "/api/v1/disciplinary-cases/{id:guid}/audit-trail",
        // Download de evidências (anexos das medidas)
        "/api/v1/disciplinary-cases/{caseId:guid}/evidences/{evidenceId:guid}/file",
        // Relatórios, dashboard e exportação CSV
        "/api/v1/disciplinary-reports/dashboard",
        "/api/v1/disciplinary-reports/cases",
        "/api/v1/disciplinary-reports/cases/export",
        "/api/v1/disciplinary-reports/measures",
        "/api/v1/disciplinary-reports/employees/{employeeId:guid}/history",
        // Notificações (as que apontam para medidas fora do escopo somem)
        "/api/v1/notifications",
        "/api/v1/notifications/unread",
        "/api/v1/notifications/unread/count",
        // Trilha geral de auditoria: tudo ou nada (só com escopo total)
        "/api/v1/audit-logs",
    };

    /// <summary>Rotas que não leem dados de funcionários nem de medidas, com o motivo.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["/api/v1"] = "informação do sistema",
        ["/api/v1/health"] = "saúde da aplicação",
        ["/api/v1/auth/me"] = "dados do próprio usuário",
        ["/api/v1/users"] = "cadastro de usuários do sistema (não de funcionários); exige Users.View",
        ["/api/v1/users/{id:guid}"] = "cadastro de usuários do sistema; exige Users.View",
        ["/api/v1/users/{id:guid}/access"] = "perfis e gerências de um usuário; exige Users.View",
        ["/api/v1/roles"] = "catálogo de perfis",
        ["/api/v1/management-units"] = "catálogo das 9 gerências (sem funcionários); exige Users.View",
        ["/api/v1/infraction-types"] = "catálogo de motivos",
        ["/api/v1/infraction-types/{id:guid}"] = "catálogo de motivos",
        ["/api/v1/employees/import/batches"] = "lotes de importação: só contagens agregadas, sem dado de funcionário; exige Employees.Import",
    };
}
