using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Common;

/// <summary>Rótulos em português dos enums do processo em etapas (usados nas exportações legadas).</summary>
public static class PortugueseLabels
{
    public static string Of(DisciplinaryCaseStatus status) => status switch
    {
        DisciplinaryCaseStatus.Draft => "Rascunho",
        DisciplinaryCaseStatus.Open => "Aberto",
        DisciplinaryCaseStatus.UnderInvestigation => "Em apuração",
        DisciplinaryCaseStatus.AwaitingDecision => "Aguardando decisão",
        DisciplinaryCaseStatus.Decided => "Decidido",
        DisciplinaryCaseStatus.Completed => "Concluído",
        DisciplinaryCaseStatus.Cancelled => "Cancelado",
        _ => "Não definido"
    };

    public static string Of(DisciplinaryCasePriority priority) => priority switch
    {
        DisciplinaryCasePriority.Low => "Baixa",
        DisciplinaryCasePriority.Normal => "Normal",
        DisciplinaryCasePriority.High => "Alta",
        DisciplinaryCasePriority.Critical => "Crítica",
        _ => "Não definida"
    };

    /// <summary>Converte o nome do enum (como vem nos DTOs legados) para o rótulo.</summary>
    public static string CaseStatus(string? value) =>
        Enum.TryParse<DisciplinaryCaseStatus>(value, out var parsed) ? Of(parsed) : value ?? string.Empty;

    public static string CasePriority(string? value) =>
        Enum.TryParse<DisciplinaryCasePriority>(value, out var parsed) ? Of(parsed) : value ?? string.Empty;
}
