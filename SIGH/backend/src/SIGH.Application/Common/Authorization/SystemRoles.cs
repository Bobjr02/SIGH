namespace SIGH.Application.Common.Authorization;

/// <summary>Perfil de sistema: nome estável (chave), rótulo exibido e permissões FIXAS.</summary>
public sealed record SystemRoleDefinition(string Name, string Label, string Description, IReadOnlyList<string> Permissions);

/// <summary>
/// Perfis semeados. As permissões de cada perfil são fixas aqui e sincronizadas pelo seed a cada
/// inicialização (o que sobrar é retirado); não há tela para editá-las. Para mudar um perfil, mude
/// esta lista (e a tabela do README).
///
/// Regras de negócio refletidas aqui:
/// - o Administrador distribui papéis mas NÃO registra nem aprova medidas;
/// - as permissões do processo em etapas (Disciplinary.Cases.*) não vão para nenhum perfil, exceto
///   as de evidência (o anexo das medidas reaproveita as evidências);
/// - Gestor não tem escopo total: só enxerga as gerências vinculadas a ele.
/// </summary>
public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string HrRegistrar = "HrRegistrar";
    public const string HrApprover = "HrApprover";
    public const string Legal = "Legal";
    public const string Manager = "Manager";

    private static readonly string[] Reports =
    [
        PermissionCodes.DisciplinaryReports.ViewDashboard,
        PermissionCodes.DisciplinaryReports.ViewCases,
        PermissionCodes.DisciplinaryReports.ViewMeasures,
        PermissionCodes.DisciplinaryReports.ViewEmployeeHistory,
        PermissionCodes.DisciplinaryReports.Export,
    ];

    private static readonly string[] OwnNotifications =
    [
        PermissionCodes.Notifications.View,
        PermissionCodes.Notifications.Read,
    ];

    public static IReadOnlyList<SystemRoleDefinition> All { get; } =
    [
        new(Administrator, "Administrador", "Distribui perfis e gerências, cadastra usuários e mantém os catálogos. Não registra nem aprova medidas.",
        [
            PermissionCodes.Users.View,
            PermissionCodes.Users.Create,
            PermissionCodes.Users.ChangeStatus,
            PermissionCodes.Users.Unlock,
            PermissionCodes.Users.ManageRoles,
            PermissionCodes.Employees.View,
            PermissionCodes.Employees.Import,
            PermissionCodes.ManagementUnits.Manage,
            PermissionCodes.InfractionTypes.View,
            PermissionCodes.InfractionTypes.Create,
            PermissionCodes.InfractionTypes.Update,
            PermissionCodes.InfractionTypes.Activate,
            PermissionCodes.InfractionTypes.Deactivate,
            PermissionCodes.Audit.View,
            PermissionCodes.DisciplinaryRecords.View,
            PermissionCodes.DisciplinaryRecords.ViewAll,
            .. Reports,
            .. OwnNotifications,
        ]),
        new(HrRegistrar, "RH – Registro", "Auxiliar de escritório: registra a medida já decidida, corrige e cancela as rejeitadas e anexa documentos.",
        [
            PermissionCodes.DisciplinaryRecords.View,
            PermissionCodes.DisciplinaryRecords.ViewAll,
            PermissionCodes.DisciplinaryRecords.Register,
            PermissionCodes.Employees.View,
            PermissionCodes.DisciplinaryCases.AddEvidence,
            .. Reports,
            .. OwnNotifications,
        ]),
        new(HrApprover, "RH – Aprovador", "Encarregado do RH: aprova ou rejeita as medidas registradas por outra pessoa.",
        [
            PermissionCodes.DisciplinaryRecords.View,
            PermissionCodes.DisciplinaryRecords.ViewAll,
            PermissionCodes.DisciplinaryRecords.Approve,
            PermissionCodes.Employees.View,
            PermissionCodes.DisciplinaryCases.AddEvidence,
            PermissionCodes.DisciplinaryCases.RemoveEvidence,
            .. Reports,
            .. OwnNotifications,
        ]),
        new(Legal, "Jurídico", "Consulta todas as medidas e a trilha de auditoria.",
        [
            PermissionCodes.DisciplinaryRecords.View,
            PermissionCodes.DisciplinaryRecords.ViewAll,
            PermissionCodes.Employees.View,
            PermissionCodes.Audit.View,
            .. Reports,
        ]),
        new(Manager, "Gestor", "Consulta as medidas e os funcionários das gerências vinculadas a ele (exige ao menos uma gerência).",
        [
            PermissionCodes.DisciplinaryRecords.View,
            PermissionCodes.Employees.View,
            .. Reports,
        ]),
    ];

    public static SystemRoleDefinition? Find(string name) => All.FirstOrDefault(r => r.Name == name);
}
