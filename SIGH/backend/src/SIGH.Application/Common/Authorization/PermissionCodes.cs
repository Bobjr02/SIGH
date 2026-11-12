namespace SIGH.Application.Common.Authorization;

/// <summary>
/// Definição de uma permissão do sistema, usada pelo seed e pela validação de consistência.
/// </summary>
public sealed record PermissionDefinition(string Code, string Name, string Description);

/// <summary>
/// Fonte única de verdade dos códigos de permissão do SIGH.
/// Todo [Permission("...")] dos controllers deve usar estas constantes, e o seed do banco
/// cadastra exatamente a lista <see cref="All"/>.
/// </summary>
public static class PermissionCodes
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string ChangeStatus = "Users.ChangeStatus";
        public const string Unlock = "Users.Unlock";
    }

    public static class Employees
    {
        public const string View = "Employees.View";
        public const string Create = "Employees.Create";
        public const string Update = "Employees.Update";
        public const string ChangeOrganizationalAssignment = "Employees.ChangeOrganizationalAssignment";
        public const string ChangeSupervisor = "Employees.ChangeSupervisor";
        public const string ChangeStatus = "Employees.ChangeStatus";
        public const string Terminate = "Employees.Terminate";
        public const string Reactivate = "Employees.Reactivate";
        public const string LinkUser = "Employees.LinkUser";
        public const string UnlinkUser = "Employees.UnlinkUser";
    }

    public static class DisciplinaryCases
    {
        public const string View = "Disciplinary.Cases.View";
        public const string Create = "Disciplinary.Cases.Create";
        public const string Open = "Disciplinary.Cases.Open";
        public const string StartInvestigation = "Disciplinary.Cases.StartInvestigation";
        public const string SubmitForDecision = "Disciplinary.Cases.SubmitForDecision";
        public const string ApproveDecision = "Disciplinary.Cases.ApproveDecision";
        public const string RejectDecision = "Disciplinary.Cases.RejectDecision";
        public const string AddOccurrence = "Disciplinary.Cases.AddOccurrence";
        public const string AddEmployee = "Disciplinary.Cases.AddEmployee";
        public const string AddEvidence = "Disciplinary.Cases.AddEvidence";
        public const string RecordDecision = "Disciplinary.Cases.RecordDecision";
        public const string ApplyMeasure = "Disciplinary.Cases.ApplyMeasure";
        public const string Cancel = "Disciplinary.Cases.Cancel";
        public const string Conclude = "Disciplinary.Cases.Conclude";
    }

    public static class InfractionTypes
    {
        public const string View = "Disciplinary.InfractionTypes.View";
        public const string Create = "Disciplinary.InfractionTypes.Create";
        public const string Update = "Disciplinary.InfractionTypes.Update";
        public const string Activate = "Disciplinary.InfractionTypes.Activate";
        public const string Deactivate = "Disciplinary.InfractionTypes.Deactivate";
    }

    public static class DisciplinaryReports
    {
        public const string ViewDashboard = "Disciplinary.Reports.ViewDashboard";
        public const string ViewCases = "Disciplinary.Reports.ViewCases";
        public const string ViewMeasures = "Disciplinary.Reports.ViewMeasures";
        public const string ViewEmployeeHistory = "Disciplinary.Reports.ViewEmployeeHistory";
        public const string Export = "Disciplinary.Reports.Export";
    }

    public static class Audit
    {
        public const string View = "Audit.View";
    }

    public static class Notifications
    {
        public const string View = "Notifications.View";
        public const string Read = "Notifications.Read";
        public const string Manage = "Notifications.Manage";
    }

    public static IReadOnlyList<PermissionDefinition> All { get; } = new List<PermissionDefinition>
    {
        // Usuários
        new(Users.View, "Visualizar usuários", "Consultar a lista e os detalhes de usuários."),
        new(Users.Create, "Criar usuários", "Cadastrar novos usuários no sistema."),
        new(Users.ChangeStatus, "Alterar status de usuários", "Ativar, inativar ou suspender usuários."),
        new(Users.Unlock, "Desbloquear usuários", "Desbloquear usuários bloqueados por tentativas de login."),

        // Funcionários
        new(Employees.View, "Visualizar funcionários", "Consultar a lista e os detalhes de funcionários."),
        new(Employees.Create, "Cadastrar funcionários", "Cadastrar novos funcionários."),
        new(Employees.Update, "Atualizar funcionários", "Atualizar os dados cadastrais de funcionários."),
        new(Employees.ChangeOrganizationalAssignment, "Alterar lotação de funcionários", "Alterar unidade, departamento e cargo de funcionários."),
        new(Employees.ChangeSupervisor, "Alterar supervisor de funcionários", "Definir ou alterar o supervisor imediato de funcionários."),
        new(Employees.ChangeStatus, "Alterar status de funcionários", "Alterar a situação funcional de funcionários."),
        new(Employees.Terminate, "Desligar funcionários", "Registrar o desligamento de funcionários."),
        new(Employees.Reactivate, "Reativar funcionários", "Reativar funcionários desligados ou inativos."),
        new(Employees.LinkUser, "Vincular usuário a funcionário", "Vincular uma conta de usuário a um funcionário."),
        new(Employees.UnlinkUser, "Desvincular usuário de funcionário", "Remover o vínculo entre usuário e funcionário."),

        // Processos disciplinares
        new(DisciplinaryCases.View, "Visualizar processos disciplinares", "Consultar a lista e os detalhes de processos disciplinares."),
        new(DisciplinaryCases.Create, "Criar processos disciplinares", "Cadastrar novos processos disciplinares."),
        new(DisciplinaryCases.Open, "Abrir processos disciplinares", "Transicionar processos de rascunho para aberto."),
        new(DisciplinaryCases.StartInvestigation, "Iniciar investigação", "Iniciar a fase de investigação de processos disciplinares."),
        new(DisciplinaryCases.SubmitForDecision, "Encaminhar para decisão", "Encaminhar processos investigados para a etapa de decisão."),
        new(DisciplinaryCases.ApproveDecision, "Aprovar decisões", "Aprovar decisões registradas em processos disciplinares."),
        new(DisciplinaryCases.RejectDecision, "Rejeitar decisões", "Rejeitar decisões registradas em processos disciplinares."),
        new(DisciplinaryCases.AddOccurrence, "Registrar ocorrências", "Registrar ocorrências em processos disciplinares."),
        new(DisciplinaryCases.AddEmployee, "Vincular funcionários a processos", "Vincular funcionários envolvidos a processos disciplinares."),
        new(DisciplinaryCases.AddEvidence, "Anexar evidências", "Anexar evidências a processos disciplinares."),
        new(DisciplinaryCases.RecordDecision, "Registrar decisões", "Registrar a decisão fundamentada de processos disciplinares."),
        new(DisciplinaryCases.ApplyMeasure, "Aplicar medidas disciplinares", "Aplicar medidas disciplinares aos funcionários envolvidos."),
        new(DisciplinaryCases.Cancel, "Cancelar processos disciplinares", "Cancelar processos disciplinares informando o motivo."),
        new(DisciplinaryCases.Conclude, "Concluir processos disciplinares", "Concluir formalmente processos disciplinares."),

        // Tipos de infração
        new(InfractionTypes.View, "Visualizar tipos de infração", "Consultar o catálogo de tipos de infração."),
        new(InfractionTypes.Create, "Criar tipos de infração", "Cadastrar novos tipos de infração."),
        new(InfractionTypes.Update, "Atualizar tipos de infração", "Atualizar tipos de infração existentes."),
        new(InfractionTypes.Activate, "Ativar tipos de infração", "Ativar tipos de infração."),
        new(InfractionTypes.Deactivate, "Inativar tipos de infração", "Inativar tipos de infração."),

        // Relatórios disciplinares
        new(DisciplinaryReports.ViewDashboard, "Visualizar painel disciplinar", "Consultar os indicadores do painel disciplinar."),
        new(DisciplinaryReports.ViewCases, "Visualizar relatório de processos", "Consultar o relatório de processos disciplinares."),
        new(DisciplinaryReports.ViewMeasures, "Visualizar relatório de medidas", "Consultar o relatório de medidas disciplinares."),
        new(DisciplinaryReports.ViewEmployeeHistory, "Visualizar histórico disciplinar", "Consultar o histórico disciplinar de funcionários."),
        new(DisciplinaryReports.Export, "Exportar relatórios disciplinares", "Exportar relatórios disciplinares em CSV."),

        // Notificações
        new(Notifications.View, "Visualizar notificações", "Consultar as próprias notificações."),
        new(Notifications.Read, "Marcar notificações como lidas", "Marcar as próprias notificações como lidas."),
        new(Notifications.Manage, "Gerenciar notificações", "Executar rotinas administrativas de notificações."),

        // Auditoria
        new(Audit.View, "Consultar auditoria", "Consultar a trilha de auditoria do sistema."),
    };
}
