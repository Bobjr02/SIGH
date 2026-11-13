using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Records;

/// <summary>Configuração do fluxo de medidas (seção "Records").</summary>
public sealed class RecordsOptions
{
    public const string SectionName = "Records";

    /// <summary>Medida pendente há mais destes dias: os aprovadores recebem um aviso por dia.</summary>
    public int PendingApprovalAlertDays { get; set; } = 3;

    /// <summary>Além do aviso no sistema, envia e-mail (pela outbox) nas notificações de medidas.</summary>
    public bool NotifyByEmail { get; set; } = true;
}

/// <summary>
/// Datas de negócio (dia da ocorrência, da medida, da suspensão) no fuso da usina (UTC-3, sem horário de
/// verão). Guardadas como DateTimeOffset à meia-noite local quando o domínio exige um instante.
/// </summary>
public static class BusinessCalendar
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    public static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.ToOffset(Offset).DateTime);

    public static DateTimeOffset StartOf(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), Offset);

    public static DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Offset).DateTime);
}

/// <summary>Situação exibida da medida.</summary>
public enum RecordStatus
{
    /// <summary>"Aguardando aprovação".</summary>
    PendingApproval = 1,
    /// <summary>Voltou para o registrante, que corrige e reenvia ou cancela.</summary>
    Rejected = 2,
    Approved = 3,
    Cancelled = 4
}

public static class RecordLabels
{
    /// <summary>Tipos aceitos no registro. TerminationRecommendation, Guidance e Other ficam de fora.</summary>
    public static readonly IReadOnlyList<DisciplinaryMeasureType> RegistrableTypes =
    [
        DisciplinaryMeasureType.VerbalWarning,
        DisciplinaryMeasureType.WrittenWarning,
        DisciplinaryMeasureType.Suspension,
        DisciplinaryMeasureType.JustCauseTermination,
    ];

    public static string Of(DisciplinaryMeasureType type) => type switch
    {
        DisciplinaryMeasureType.VerbalWarning => "Advertência verbal",
        DisciplinaryMeasureType.WrittenWarning => "Advertência escrita",
        DisciplinaryMeasureType.Suspension => "Suspensão",
        DisciplinaryMeasureType.JustCauseTermination => "Demissão por justa causa",
        DisciplinaryMeasureType.TerminationRecommendation => "Recomendação de desligamento",
        DisciplinaryMeasureType.Guidance => "Orientação",
        _ => "Outra"
    };

    public static string Of(RecordStatus status) => status switch
    {
        RecordStatus.PendingApproval => "Aguardando aprovação",
        RecordStatus.Rejected => "Rejeitada",
        RecordStatus.Approved => "Aprovada",
        RecordStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };
}

/// <summary>Registro (POST) e correção (PUT) usam os mesmos campos.</summary>
public sealed record RecordInput(
    Guid EmployeeId,
    Guid InfractionTypeId,
    DisciplinaryMeasureType MeasureType,
    IReadOnlyList<DateOnly> OccurrenceDates,
    DateOnly MeasureDate,
    DateOnly? SuspensionStart,
    DateOnly? SuspensionEnd,
    string Description,
    /// <summary>Confirma o registro mesmo com aviso de possível duplicidade.</summary>
    bool ConfirmPossibleDuplicate = false);

public sealed record ReasonInput(string Reason);

public sealed record PossibleDuplicateDto(Guid Id, string CaseNumber, RecordStatus Status, DateOnly OccurrenceDate);

/// <summary>Resultado do registro/correção: ou a medida, ou o aviso de possível duplicidade.</summary>
public sealed record RecordSaveResult(RecordDetailDto? Record, IReadOnlyList<PossibleDuplicateDto> PossibleDuplicates)
{
    public bool RequiresDuplicateConfirmation => Record is null && PossibleDuplicates.Count > 0;
}

public sealed record RecordSummaryDto(
    Guid Id,
    string CaseNumber,
    RecordStatus Status,
    string StatusLabel,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    string? ManagementUnitCode,
    string? ManagementUnitName,
    string? DepartmentName,
    DisciplinaryMeasureType MeasureType,
    string MeasureTypeLabel,
    Guid? InfractionTypeId,
    string? InfractionTypeName,
    DateOnly? MeasureDate,
    DateOnly? FirstOccurrenceDate,
    int? SuspensionDays,
    DateTimeOffset RegisteredAt,
    Guid RegisteredByUserId,
    string? RegisteredByName);

public sealed record RecordEvidenceDto(
    Guid Id, string Title, string? Description, string EvidenceType, bool HasFile, string? OriginalFileName,
    string? ContentType, long? FileSize, DateTimeOffset CollectedAt, Guid CollectedByUserId);

public sealed record RecordDecisionHistoryDto(
    Guid Id, DecisionStatus Status, DisciplinaryMeasureType? MeasureType, Guid DecidedByUserId, string? DecidedByName,
    DateTimeOffset DecidedAt, Guid? ApprovedByUserId, string? ApprovedByName, DateTimeOffset? ApprovedAt,
    Guid? RejectedByUserId, string? RejectedByName, DateTimeOffset? RejectedAt, string? RejectionReason);

public sealed record RecordActionsDto(bool CanApprove, bool CanReject, bool CanCorrect, bool CanCancel, bool CanAttach, bool CanRemoveEvidence);

public sealed record RecordDetailDto(
    RecordSummaryDto Summary,
    string Description,
    IReadOnlyList<DateOnly> OccurrenceDates,
    DateOnly? SuspensionStart,
    DateOnly? SuspensionEnd,
    string? ManagementUnitCodeAtTime,
    DateTimeOffset? ApprovedAt,
    string? ApprovedByName,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    string? LastRejectionReason,
    string? LastRejectedByName,
    DateTimeOffset? LastRejectedAt,
    IReadOnlyList<RecordEvidenceDto> Evidences,
    IReadOnlyList<RecordDecisionHistoryDto> History,
    RecordActionsDto Actions);

public sealed record GetRecordsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    RecordStatus? Status = null,
    DisciplinaryMeasureType? MeasureType = null,
    Guid? InfractionTypeId = null,
    Guid? ManagementUnitId = null,
    Guid? DepartmentId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    /// <summary>Nome ou matrícula do funcionário.</summary>
    string? Employee = null,
    /// <summary>Só as medidas registradas pelo usuário (página inicial do registrante).</summary>
    bool Mine = false);

public sealed record OptionDto(Guid Id, string? Code, string Name);

public sealed record MeasureTypeOptionDto(DisciplinaryMeasureType Value, string Label);

/// <summary>Opções dos filtros e do formulário, já mascaradas pelo escopo do usuário.</summary>
public sealed record RecordOptionsDto(
    IReadOnlyList<MeasureTypeOptionDto> MeasureTypes,
    IReadOnlyList<OptionDto> InfractionTypes,
    IReadOnlyList<OptionDto> ManagementUnits,
    IReadOnlyList<OptionDto> Departments);
