namespace SIGH.Application.Options;

public class NotificationReminderOptions
{
    public const string SectionName = "NotificationReminderOptions";

    /// <summary>
    /// Antecedência em dias para emissão do primeiro alerta antes do vencimento.
    /// Default: 2 dias.
    /// </summary>
    public int FirstAlertDaysBeforeDue { get; set; } = 2;

    /// <summary>
    /// Intervalo em horas entre lembretes recorrentes para o mesmo item.
    /// Default: 24 horas.
    /// </summary>
    public int ReminderIntervalHours { get; set; } = 24;

    /// <summary>
    /// Quantidade máxima de lembretes recorrentes a serem enviados antes de esgotar o ciclo.
    /// Default: 3 lembretes.
    /// </summary>
    public int MaxRemindersCount { get; set; } = 3;

    /// <summary>
    /// Atraso mínimo em dias após o vencimento do prazo para iniciar o escalonamento automático.
    /// Default: 2 dias.
    /// </summary>
    public int EscalationDelayDays { get; set; } = 2;

    /// <summary>
    /// Limite máximo de níveis de escalonamento (1: Responsável, 2: Gestor Direto, 3: Gestor Superior/Nível 3).
    /// Default: 3 níveis.
    /// </summary>
    public int MaxEscalationLevels { get; set; } = 3;
}
