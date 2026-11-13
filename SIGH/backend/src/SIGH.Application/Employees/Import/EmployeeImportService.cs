using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Domain.Employees.Entities;

namespace SIGH.Application.Employees.Import;

public class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>Acima deste percentual de ativos desligados por ausência, a confirmação exige confirmMassTermination=true.</summary>
    public double MaxTerminationPercent { get; set; } = 10;

    /// <summary>Por quanto tempo a pré-visualização pode ser confirmada.</summary>
    public int PreviewValidityMinutes { get; set; } = 30;
}

public static class EmployeeImportErrors
{
    public const string InvalidFile = "EmployeeImport.InvalidFile";
    public const string BatchNotFound = "EmployeeImport.Batch.NotFound";
    public const string BatchExpired = "EmployeeImport.Batch.Expired";
    public const string BatchAlreadyApplied = "EmployeeImport.Batch.AlreadyApplied";
    public const string FileMismatch = "EmployeeImport.FileMismatch";
    public const string PlanChanged = "EmployeeImport.PlanChanged";
    public const string MassTerminationNotConfirmed = "EmployeeImport.MassTerminationNotConfirmed";
}

public sealed record ImportIssueDto(int? Row, string? EmployeeNumber, string Severity, string Message);
public sealed record ImportChangeDto(string EmployeeNumber, string FullName, IReadOnlyList<FieldChange> Changes);

public sealed record ImportSummaryDto(
    int TotalRows, int Created, int Updated, int Terminated, int TerminatedByAbsence, int Reactivated, int Unchanged, int Errors, int Warnings);

public sealed record EmployeeImportPreviewDto(
    Guid BatchId,
    string FileName,
    string FileSha256,
    DateTimeOffset ExpiresAt,
    ImportSummaryDto Summary,
    IReadOnlyList<ImportChangeDto> Created,
    IReadOnlyList<ImportChangeDto> Updated,
    IReadOnlyList<ImportChangeDto> ToTerminate,
    IReadOnlyList<ImportChangeDto> Reactivated,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<ImportIssueDto> Errors,
    IReadOnlyList<ImportIssueDto> Warnings,
    IReadOnlyList<string> NewManagementUnits,
    int ActiveBefore,
    double AbsenceTerminationPercent,
    double MaxTerminationPercent,
    bool RequiresMassTerminationConfirmation);

public sealed record EmployeeImportResultDto(Guid BatchId, ImportSummaryDto Summary, DateTimeOffset AppliedAt);

public sealed record EmployeeImportBatchDto(
    Guid Id, string FileName, string FileSha256, string Status, DateTimeOffset PreviewedAt, string? PreviewedByName,
    DateTimeOffset? AppliedAt, string? AppliedByName, ImportSummaryDto Summary, bool MassTerminationConfirmed);

/// <summary>Acesso ao banco da importação (implementado na Persistence).</summary>
public interface IEmployeeImportStore
{
    Guid CompanyId { get; }
    Task<(IReadOnlyList<EmployeeSnapshot> Employees, ReferenceSnapshot References)> LoadSnapshotAsync(CancellationToken cancellationToken);
    Task<EmployeeImportBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken);
    Task AddBatchAsync(EmployeeImportBatch batch, CancellationToken cancellationToken);
    /// <summary>Aplica o plano e marca o lote como aplicado, numa transação.</summary>
    Task ApplyAsync(EmployeeImportPlan plan, EmployeeImportBatch batch, DateOnly importDate, Guid actorUserId, bool massTerminationConfirmed, CancellationToken cancellationToken);
    Task<PagedResult<EmployeeImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public interface IEmployeeImportService
{
    Task<Result<EmployeeImportPreviewDto>> PreviewAsync(Stream file, string fileName, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeImportResultDto>> ConfirmAsync(Guid batchId, Stream file, bool confirmMassTermination, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Importação em duas etapas.
/// 1. Pré-visualização (dry-run): lê e valida o arquivo, calcula o plano e cria um lote "Previewed" com
///    o SHA-256 do arquivo, a impressão digital do plano e a validade. Nenhum funcionário é alterado.
/// 2. Confirmação: recebe o id do lote e o MESMO arquivo (o arquivo não é guardado). Confere o hash e a
///    validade, recalcula o plano e só aplica se ele for idêntico ao pré-visualizado (a base não mudou),
///    numa transação.
/// </summary>
public sealed class EmployeeImportService : IEmployeeImportService
{
    private static readonly TimeZoneInfo Brasilia = FindBrasilia();

    private readonly IHrSpreadsheetReader _reader;
    private readonly IEmployeeImportStore _store;
    private readonly IDateTimeProvider _clock;
    private readonly ImportOptions _options;
    private readonly ILogger<EmployeeImportService> _logger;

    public EmployeeImportService(
        IHrSpreadsheetReader reader,
        IEmployeeImportStore store,
        IDateTimeProvider clock,
        IOptions<ImportOptions> options,
        ILogger<EmployeeImportService> logger)
    {
        _reader = reader;
        _store = store;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<EmployeeImportPreviewDto>> PreviewAsync(Stream file, string fileName, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var (hash, plan, error) = await BuildPlanAsync(file, cancellationToken);
        if (plan is null)
            return Result<EmployeeImportPreviewDto>.Failure(error!, EmployeeImportErrors.InvalidFile);

        var now = _clock.UtcNow;
        var batch = EmployeeImportBatch.CreatePreview(fileName, hash, actorUserId, now,
            TimeSpan.FromMinutes(Math.Max(1, _options.PreviewValidityMinutes)), Counts(plan), plan.Fingerprint, IssuesJson(plan));
        await _store.AddBatchAsync(batch, cancellationToken);

        // Só contagens no log: nunca nomes, CPFs ou conteúdo de células.
        _logger.LogInformation("Importação de funcionários: pré-visualização {BatchId} ({Rows} linhas, {Errors} erros).",
            batch.Id, plan.TotalRows, plan.ErrorCount);

        return Result<EmployeeImportPreviewDto>.Ok(ToPreview(batch, plan));
    }

    public async Task<Result<EmployeeImportResultDto>> ConfirmAsync(Guid batchId, Stream file, bool confirmMassTermination, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var batch = await _store.GetBatchAsync(batchId, cancellationToken);
        if (batch is null)
            return Result<EmployeeImportResultDto>.Failure("Pré-visualização não encontrada.", EmployeeImportErrors.BatchNotFound);
        if (batch.Status == EmployeeImportBatchStatus.Applied)
            return Result<EmployeeImportResultDto>.Failure("Esta importação já foi confirmada.", EmployeeImportErrors.BatchAlreadyApplied);
        if (batch.IsExpired(_clock.UtcNow))
            return Result<EmployeeImportResultDto>.Failure("A pré-visualização expirou. Envie o arquivo de novo.", EmployeeImportErrors.BatchExpired);

        var (hash, plan, error) = await BuildPlanAsync(file, cancellationToken);
        if (plan is null)
            return Result<EmployeeImportResultDto>.Failure(error!, EmployeeImportErrors.InvalidFile);
        if (!string.Equals(hash, batch.FileSha256, StringComparison.OrdinalIgnoreCase))
            return Result<EmployeeImportResultDto>.Failure("O arquivo enviado não é o mesmo da pré-visualização.", EmployeeImportErrors.FileMismatch);
        if (plan.Fingerprint != batch.PlanFingerprint)
            return Result<EmployeeImportResultDto>.Failure(
                "A base de funcionários mudou desde a pré-visualização. Faça a pré-visualização de novo.", EmployeeImportErrors.PlanChanged);
        if (plan.RequiresMassTerminationConfirmation && !confirmMassTermination)
            return Result<EmployeeImportResultDto>.Failure(
                $"A importação desliga {plan.Of(EmployeeChangeKind.TerminatedByAbsence).Count()} funcionários ausentes do arquivo " +
                $"({plan.AbsenceTerminationPercent:0.##}% dos ativos, acima do limite de {_options.MaxTerminationPercent:0.##}%). " +
                "Confirme o desligamento em massa para continuar.",
                EmployeeImportErrors.MassTerminationNotConfirmed);

        await _store.ApplyAsync(plan, batch, ImportDate(), actorUserId, confirmMassTermination, cancellationToken);

        _logger.LogInformation("Importação de funcionários {BatchId} aplicada: {Created} novos, {Updated} alterados, {Terminated} desligados, {Absent} desligados por ausência, {Reactivated} reativados.",
            batch.Id, plan.Of(EmployeeChangeKind.Created).Count(), plan.Of(EmployeeChangeKind.Updated).Count(),
            plan.Of(EmployeeChangeKind.Terminated).Count(), plan.Of(EmployeeChangeKind.TerminatedByAbsence).Count(),
            plan.Of(EmployeeChangeKind.Reactivated).Count());

        return Result<EmployeeImportResultDto>.Ok(new EmployeeImportResultDto(batch.Id, Summary(Counts(plan)), batch.AppliedAt!.Value));
    }

    public Task<PagedResult<EmployeeImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        _store.ListBatchesAsync(pageNumber, pageSize, cancellationToken);

    /// <summary>Data da importação no fuso de Brasília (usada como Data demissão dos ausentes).</summary>
    private DateOnly ImportDate() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_clock.UtcNow, Brasilia).DateTime);

    private async Task<(string Hash, EmployeeImportPlan? Plan, string? Error)> BuildPlanAsync(Stream file, CancellationToken cancellationToken)
    {
        // Lê para memória: precisamos do hash e de um stream seekable para o leitor.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(buffer)).ToLowerInvariant();
        buffer.Position = 0;

        HrSheet sheet;
        try
        {
            sheet = _reader.Read(buffer);
        }
        catch (HrSpreadsheetException ex)
        {
            return (hash, null, ex.Message);
        }

        var (employees, references) = await _store.LoadSnapshotAsync(cancellationToken);
        var plan = EmployeeImportPlanner.Plan(sheet.Rows, sheet.Warnings, employees, references, ImportDate(), _options.MaxTerminationPercent);
        return (hash, plan, null);
    }

    private static EmployeeImportCounts Counts(EmployeeImportPlan plan) => new(
        plan.TotalRows,
        plan.Of(EmployeeChangeKind.Created).Count(),
        plan.Of(EmployeeChangeKind.Updated).Count(),
        plan.Of(EmployeeChangeKind.Terminated).Count(),
        plan.Of(EmployeeChangeKind.TerminatedByAbsence).Count(),
        plan.Of(EmployeeChangeKind.Reactivated).Count(),
        plan.UnchangedEmployeeNumbers.Count,
        plan.ErrorCount,
        plan.WarningCount);

    public static ImportSummaryDto Summary(EmployeeImportCounts c) =>
        new(c.TotalRows, c.Created, c.Updated, c.Terminated, c.TerminatedByAbsence, c.Reactivated, c.Unchanged, c.Errors, c.Warnings);

    /// <summary>Erros e avisos para o lote: linha, matrícula e mensagem (as mensagens nunca contêm CPF nem colunas sensíveis).</summary>
    private static string? IssuesJson(EmployeeImportPlan plan) => plan.Issues.Count == 0
        ? null
        : JsonSerializer.Serialize(plan.Issues.Select(i => new { row = i.RowNumber, employeeNumber = i.EmployeeNumber, severity = i.Severity.ToString(), message = i.Message }));

    private EmployeeImportPreviewDto ToPreview(EmployeeImportBatch batch, EmployeeImportPlan plan)
    {
        static ImportChangeDto Dto(PlannedChange c) => new(c.EmployeeNumber, c.FullName, c.Changes);
        static ImportIssueDto Issue(HrImportIssue i) => new(i.RowNumber, i.EmployeeNumber, i.Severity.ToString(), i.Message);

        return new EmployeeImportPreviewDto(
            batch.Id, batch.FileName, batch.FileSha256, batch.ExpiresAt, Summary(Counts(plan)),
            plan.Of(EmployeeChangeKind.Created).Select(Dto).ToList(),
            // "Alterados" inclui os desligados pelo próprio status do arquivo (com o campo Situação na lista de mudanças).
            plan.Changes.Where(c => c.Kind is EmployeeChangeKind.Updated or EmployeeChangeKind.Terminated).Select(Dto).ToList(),
            plan.Of(EmployeeChangeKind.TerminatedByAbsence).Select(Dto).ToList(),
            plan.Of(EmployeeChangeKind.Reactivated).Select(Dto).ToList(),
            plan.UnchangedEmployeeNumbers,
            plan.Issues.Where(i => i.Severity == HrIssueSeverity.Error).Select(Issue).ToList(),
            plan.Issues.Where(i => i.Severity == HrIssueSeverity.Warning).Select(Issue).ToList(),
            plan.NewManagementUnits,
            plan.ActiveBefore,
            plan.AbsenceTerminationPercent,
            _options.MaxTerminationPercent,
            plan.RequiresMassTerminationConfirmation);
    }

    private static TimeZoneInfo FindBrasilia()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz)) return tz;
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
