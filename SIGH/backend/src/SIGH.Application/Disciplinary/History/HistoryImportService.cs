using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Employees.Import;
using SIGH.Application.Interfaces;
using SIGH.Application.Security;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.History;

/// <summary>Seção "History".</summary>
public sealed class HistoryImportOptions
{
    public const string SectionName = "History";

    /// <summary>Por quanto tempo a pré-visualização pode ser confirmada.</summary>
    public int PreviewValidityMinutes { get; set; } = 30;

    /// <summary>
    /// History:ImportClosed. Nulo: vale o estado gravado (encerrado pela tela). true/false: aplicado e auditado no
    /// startup — é o ÚNICO jeito de reabrir a importação depois de encerrada.
    /// </summary>
    public bool? ImportClosed { get; set; }
}

public static class HistoryImportErrors
{
    public const string Closed = "HistoryImport.Closed";
    public const string InvalidFile = "HistoryImport.InvalidFile";
    public const string BatchNotFound = "HistoryImport.Batch.NotFound";
    public const string BatchExpired = "HistoryImport.Batch.Expired";
    public const string BatchAlreadyApplied = "HistoryImport.Batch.AlreadyApplied";
    public const string FileMismatch = "HistoryImport.FileMismatch";
    public const string PlanChanged = "HistoryImport.PlanChanged";

    public const string ClosedMessage =
        "A importação do histórico disciplinar foi encerrada: o SIGH é agora a única fonte das medidas. " +
        "Ela só pode ser reaberta pela configuração History:ImportClosed=false (com auditoria no startup).";
}

public static class HistoryAuditActions
{
    public const string ImportClosed = "History.ImportClosed";
    public const string ImportClosedByConfiguration = "History.ImportClosedByConfiguration";
    public const string ImportReopenedByConfiguration = "History.ImportReopenedByConfiguration";
    public const string ImportApplied = "History.ImportApplied";
}

public sealed record HistoryImportSummaryDto(int TotalRows, int ToImport, int Existing, int ToUpdate, int Rejected, int Warnings, int NewInfractionTypes, int Absent);

public sealed record HistoryRowDto(
    int Row, string EmployeeNumber, string EmployeeName, DateOnly Date, DisciplinaryMeasureType MeasureType, string MeasureTypeLabel,
    string InfractionName, bool NewInfractionType, string? ManagementUnitCode, string ManagementUnitName,
    DateOnly? PeriodStart, DateOnly? PeriodEnd, int? SuspensionDays, bool DaysUnknown);

public sealed record HistoryUpdateDto(HistoryRowDto Row, Guid CaseId, string CaseNumber, IReadOnlyList<HistoryFieldChange> Changes);

public sealed record HistoryAbsentDto(Guid CaseId, string CaseNumber, string EmployeeNumber, DateOnly Date, string MeasureTypeLabel, string InfractionName, DateTimeOffset? ImportedAt);

public sealed record HistoryImportPreviewDto(
    Guid BatchId, string FileName, string FileSha256, DateTimeOffset ExpiresAt, HistoryImportSummaryDto Summary,
    IReadOnlyList<HistoryRowDto> ToImport, IReadOnlyList<HistoryRowDto> Existing, IReadOnlyList<HistoryUpdateDto> ToUpdate,
    IReadOnlyList<string> NewInfractionTypes, IReadOnlyList<HistoryIssue> Warnings, IReadOnlyList<HistoryIssue> Rejected,
    IReadOnlyList<HistoryAbsentDto> Absent, IReadOnlyList<string> FileWarnings);

public sealed record HistoryImportResultDto(Guid BatchId, HistoryImportSummaryDto Summary, int Imported, int Updated, int InfractionTypesCreated, DateTimeOffset AppliedAt);

public sealed record HistoryImportBatchDto(
    Guid Id, string FileName, string FileSha256, string Status, DateTimeOffset PreviewedAt, string? PreviewedByName,
    DateTimeOffset? AppliedAt, string? AppliedByName, HistoryImportSummaryDto Summary);

public sealed record HistoryImportStatusDto(bool IsClosed, DateTimeOffset? ClosedAt, string? ClosedByName);

public interface IHistoryImportService
{
    Task<HistoryImportStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<HistoryImportPreviewDto>> PreviewAsync(Stream file, string fileName, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Result<HistoryImportResultDto>> ConfirmAsync(Guid batchId, Stream file, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<HistoryImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<HistoryImportStatusDto>> CloseAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>Startup: aplica History:ImportClosed (se configurado) e audita a mudança.</summary>
    Task SyncStateWithConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Importação do histórico disciplinar em duas etapas (pré-visualização → confirmação com o mesmo arquivo),
/// como a importação de funcionários. O arquivo original não é guardado; o lote guarda hash, quem e quando.
/// Cada linha vira um DisciplinaryCase Historical, sem decisão nem aprovação.
/// </summary>
public sealed class HistoryImportService : IHistoryImportService
{
    private readonly IApplicationDbContext _db;
    private readonly ISpreadsheetTableReader _reader;
    private readonly IAuthorizedCompanyProvider _company;
    private readonly IDateTimeProvider _clock;
    private readonly ISecurityAuditLogger _audit;
    private readonly HistoryImportOptions _options;

    public HistoryImportService(
        IApplicationDbContext db, ISpreadsheetTableReader reader, IAuthorizedCompanyProvider company, IDateTimeProvider clock,
        ISecurityAuditLogger audit, IOptions<HistoryImportOptions> options)
    {
        _db = db;
        _reader = reader;
        _company = company;
        _clock = clock;
        _audit = audit;
        _options = options.Value;
    }

    // ---- estado ----

    public async Task<HistoryImportStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var state = await _db.HistoryImportStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == HistoryImportState.SingletonId, cancellationToken);
        if (state is null || !state.IsClosed)
            return new HistoryImportStatusDto(false, null, null);
        var name = state.ClosedByUserId is { } id
            ? await _db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
            : "configuração (History:ImportClosed)";
        return new HistoryImportStatusDto(true, state.ClosedAt, name);
    }

    public async Task<Result<HistoryImportStatusDto>> CloseAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var state = await LoadStateAsync(cancellationToken);
        if (state.IsClosed)
            return Result<HistoryImportStatusDto>.Failure(HistoryImportErrors.ClosedMessage, HistoryImportErrors.Closed);

        state.Close(actorUserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(HistoryAuditActions.ImportClosed, actorUserId, new { closedAt = state.ClosedAt }, cancellationToken,
            nameof(HistoryImportState), state.Id.ToString());
        return Result<HistoryImportStatusDto>.Ok(await GetStatusAsync(cancellationToken), "Importação de histórico encerrada.");
    }

    public async Task SyncStateWithConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ImportClosed is not { } configured)
            return;

        var state = await LoadStateAsync(cancellationToken);
        if (state.IsClosed == configured)
            return;

        if (configured) state.Close(null, _clock.UtcNow);
        else state.ReopenByConfiguration();
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(configured ? HistoryAuditActions.ImportClosedByConfiguration : HistoryAuditActions.ImportReopenedByConfiguration,
            null, new { setting = "History:ImportClosed", value = configured }, cancellationToken, nameof(HistoryImportState), state.Id.ToString());
    }

    private async Task<HistoryImportState> LoadStateAsync(CancellationToken cancellationToken)
    {
        var state = await _db.HistoryImportStates.FirstOrDefaultAsync(s => s.Id == HistoryImportState.SingletonId, cancellationToken);
        if (state is not null)
            return state;
        state = HistoryImportState.CreateOpen();
        _db.HistoryImportStates.Add(state);
        return state;
    }

    private async Task<bool> IsClosedAsync(CancellationToken cancellationToken) =>
        await _db.HistoryImportStates.AsNoTracking().AnyAsync(s => s.Id == HistoryImportState.SingletonId && s.IsClosed, cancellationToken);

    // ---- pré-visualização e confirmação ----

    public async Task<Result<HistoryImportPreviewDto>> PreviewAsync(Stream file, string fileName, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (await IsClosedAsync(cancellationToken))
            return Result<HistoryImportPreviewDto>.Failure(HistoryImportErrors.ClosedMessage, HistoryImportErrors.Closed);

        var (hash, plan, fileWarnings, error) = await BuildPlanAsync(file, cancellationToken);
        if (plan is null)
            return Result<HistoryImportPreviewDto>.Failure(error!, HistoryImportErrors.InvalidFile);

        var now = _clock.UtcNow;
        var batch = DisciplinaryHistoryImportBatch.CreatePreview(fileName, hash, actorUserId, now,
            TimeSpan.FromMinutes(Math.Max(1, _options.PreviewValidityMinutes)), Counts(plan), plan.Fingerprint, IssuesJson(plan));
        _db.DisciplinaryHistoryImportBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<HistoryImportPreviewDto>.Ok(ToPreview(batch, plan, fileWarnings), "Pré-visualização pronta. Nada foi gravado.");
    }

    public async Task<Result<HistoryImportResultDto>> ConfirmAsync(Guid batchId, Stream file, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (await IsClosedAsync(cancellationToken))
            return Result<HistoryImportResultDto>.Failure(HistoryImportErrors.ClosedMessage, HistoryImportErrors.Closed);

        var batch = await _db.DisciplinaryHistoryImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
            return Result<HistoryImportResultDto>.Failure("Lote de importação não encontrado.", HistoryImportErrors.BatchNotFound);
        if (batch.Status == HistoryImportBatchStatus.Applied)
            return Result<HistoryImportResultDto>.Failure("Este lote já foi aplicado.", HistoryImportErrors.BatchAlreadyApplied);
        if (batch.IsExpired(_clock.UtcNow))
            return Result<HistoryImportResultDto>.Failure("A pré-visualização expirou. Envie o arquivo de novo.", HistoryImportErrors.BatchExpired);

        var (hash, plan, _, error) = await BuildPlanAsync(file, cancellationToken);
        if (plan is null)
            return Result<HistoryImportResultDto>.Failure(error!, HistoryImportErrors.InvalidFile);
        if (!string.Equals(hash, batch.FileSha256, StringComparison.OrdinalIgnoreCase))
            return Result<HistoryImportResultDto>.Failure("O arquivo enviado não é o mesmo da pré-visualização.", HistoryImportErrors.FileMismatch);
        if (plan.Fingerprint != batch.PlanFingerprint)
            return Result<HistoryImportResultDto>.Failure("Os dados mudaram desde a pré-visualização. Faça a pré-visualização de novo.", HistoryImportErrors.PlanChanged);

        var now = _clock.UtcNow;
        var companyId = await _company.GetAuthorizedCompanyIdAsync(null, cancellationToken);

        // Motivos novos: criados ativos, com código gerado a partir do nome.
        var created = new Dictionary<string, InfractionType>(StringComparer.Ordinal);
        var usedCodes = (await _db.InfractionTypes.IgnoreQueryFilters().Select(i => i.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in plan.NewInfractionTypes)
        {
            var type = InfractionType.Create(NewCode(name, usedCodes), Truncate(name.Trim(), 100), InfractionSeverity.Moderate,
                description: "Criado pela importação do histórico disciplinar.");
            _db.InfractionTypes.Add(type);
            created[HistoryKeys.Normalize(name)] = type;
        }

        foreach (var planned in plan.ToImport)
        {
            var row = planned.Row;
            var infraction = planned.Infraction is { } known
                ? (known.Id, Severity: await SeverityOfAsync(known.Id, cancellationToken))
                : (created[row.InfractionKey].Id, Severity: created[row.InfractionKey].DefaultSeverity);
            var measureDate = BusinessCalendar.StartOf(row.Date);
            var effectiveFrom = BusinessCalendar.StartOf(row.PeriodStart ?? row.Date);
            DateTimeOffset? effectiveUntil = row.PeriodEnd is { } end ? BusinessCalendar.StartOf(end).AddDays(1).AddTicks(-1) : null;

            var record = DisciplinaryCase.CreateHistorical(
                caseNumber: $"HIST-{row.Date:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                companyId: companyId,
                title: $"Histórico – {RecordLabels.Of(row.MeasureType)} – {row.EmployeeNumber}",
                description: Truncate($"Medida histórica importada da planilha de penalidades: {row.TypeText} — {row.InfractionName}.", 4000),
                historyKey: row.Key,
                importBatchId: batch.Id,
                importedByUserId: actorUserId,
                importedAt: now,
                employeeId: planned.Employee.Id,
                managementUnitIdAtTime: planned.UnitAtTime.Id,
                infractionTypeId: infraction.Item1,
                severity: infraction.Severity,
                measureType: row.MeasureType,
                measureDate: measureDate,
                effectiveFrom: effectiveFrom,
                effectiveUntil: effectiveUntil,
                suspensionDays: row.SuspensionDays);
            _db.DisciplinaryCases.Add(record);
        }

        // Reimportação de planilha corrigida: mesma chave, dados diferentes → corrige (auditoria campo a campo).
        foreach (var update in plan.ToUpdate)
        {
            var record = await _db.DisciplinaryCases.Include(c => c.Employees).Include(c => c.Measures)
                .SingleAsync(c => c.Id == update.Existing.CaseId, cancellationToken);
            var row = update.Planned.Row;
            record.ApplyHistoricalCorrection(
                update.Planned.UnitAtTime.Id,
                BusinessCalendar.StartOf(row.PeriodStart ?? row.Date),
                row.PeriodEnd is { } end ? BusinessCalendar.StartOf(end).AddDays(1).AddTicks(-1) : null,
                row.SuspensionDays,
                batch.Id);
        }

        var counts = Counts(plan);
        batch.MarkApplied(actorUserId, now, counts);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(HistoryAuditActions.ImportApplied, actorUserId,
            new { batchId = batch.Id, fileSha256 = batch.FileSha256, imported = plan.ToImport.Count, updated = plan.ToUpdate.Count, newInfractionTypes = created.Count },
            cancellationToken, nameof(DisciplinaryHistoryImportBatch), batch.Id.ToString());

        return Result<HistoryImportResultDto>.Ok(
            new HistoryImportResultDto(batch.Id, Summary(counts), plan.ToImport.Count, plan.ToUpdate.Count, created.Count, now),
            $"Histórico importado: {plan.ToImport.Count} registro(s) novo(s), {plan.ToUpdate.Count} corrigido(s).");
    }

    public async Task<PagedResult<HistoryImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.DisciplinaryHistoryImportBatches.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var batches = await query.OrderByDescending(b => b.PreviewedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var userIds = batches.SelectMany(b => new[] { (Guid?)b.PreviewedByUserId, b.AppliedByUserId }).Where(x => x is not null).Select(x => x!.Value).Distinct().ToList();
        var names = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var items = batches.Select(b => new HistoryImportBatchDto(
            b.Id, b.FileName, b.FileSha256, b.Status.ToString(), b.PreviewedAt, names.GetValueOrDefault(b.PreviewedByUserId),
            b.AppliedAt, b.AppliedByUserId is { } a ? names.GetValueOrDefault(a) : null,
            new HistoryImportSummaryDto(b.TotalRows, b.ImportedCount, b.ExistingCount, b.UpdatedCount, b.RejectedCount, b.WarningCount, b.NewInfractionTypeCount, b.AbsentCount)))
            .ToList();
        return new PagedResult<HistoryImportBatchDto>(items, pageNumber, pageSize, total);
    }

    // ---- plano ----

    private async Task<(string Hash, HistoryImportPlan? Plan, IReadOnlyList<string> FileWarnings, string? Error)> BuildPlanAsync(Stream file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
            return (string.Empty, null, [], "O arquivo está vazio.");
        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();

        SheetTable table;
        try
        {
            buffer.Position = 0;
            table = _reader.Read(buffer, HistoryColumns.All, HistoryColumns.EmployeeNumber);
        }
        catch (HrSpreadsheetException ex)
        {
            return (hash, null, [], ex.Message);
        }

        var parsed = table.Rows.Select(r =>
        {
            var (row, error) = HistoryRowParser.Parse(r);
            var number = r.Get(HistoryColumns.EmployeeNumber).Text.Trim();
            return (r.RowNumber, (string?)(number.Length == 0 ? null : number), row, error);
        }).ToList();

        return (hash, HistoryImportPlanner.Plan(parsed, await LoadReferenceAsync(cancellationToken)), table.Warnings, null);
    }

    private async Task<HistoryReference> LoadReferenceAsync(CancellationToken cancellationToken)
    {
        var employees = await _db.Employees.AsNoTracking()
            .Select(e => new HistoryEmployeeRef(e.Id, e.EmployeeNumber, e.FullName, e.ManagementUnitId))
            .ToListAsync(cancellationToken);
        var units = await _db.ManagementUnits.AsNoTracking().Select(m => new HistoryUnitRef(m.Id, m.Code, m.Name)).ToListAsync(cancellationToken);
        var infractions = await _db.InfractionTypes.AsNoTracking().Select(i => new HistoryInfractionRef(i.Id, i.Name)).ToListAsync(cancellationToken);

        var historical = await (
            from c in _db.DisciplinaryCases.AsNoTracking()
            where c.Kind == DisciplinaryCaseKind.Historical && c.HistoryKey != null
            let accused = c.Employees.FirstOrDefault(e => e.Role == CaseEmployeeRole.Accused)
            let measure = c.Measures.FirstOrDefault()
            let occurrence = c.Occurrences.FirstOrDefault()
            join emp in _db.Employees.AsNoTracking() on accused!.EmployeeId equals emp.Id
            join inf in _db.InfractionTypes.AsNoTracking() on occurrence!.InfractionTypeId equals inf.Id
            join batch in _db.DisciplinaryHistoryImportBatches.AsNoTracking() on c.HistoryImportBatchId equals batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            select new
            {
                c.Id, c.CaseNumber, c.HistoryKey, EmployeeId = emp.Id, emp.EmployeeNumber, Date = occurrence.OccurrenceDate,
                measure!.MeasureType, InfractionName = inf.Name, accused.ManagementUnitIdAtTime, measure.EffectiveFrom, measure.EffectiveUntil,
                measure.SuspensionDays, ImportedAt = (DateTimeOffset?)batch.AppliedAt
            }).ToListAsync(cancellationToken);

        var existing = historical.Select(h => new ExistingHistorical(
                h.Id, h.CaseNumber, h.HistoryKey!, h.EmployeeId, h.EmployeeNumber, BusinessCalendar.DateOf(h.Date), h.MeasureType, h.InfractionName,
                h.ManagementUnitIdAtTime,
                h.EffectiveUntil is null ? null : BusinessCalendar.DateOf(h.EffectiveFrom),
                h.EffectiveUntil is { } until ? BusinessCalendar.DateOf(until) : null,
                h.SuspensionDays, h.ImportedAt))
            .ToDictionary(h => h.Key, StringComparer.Ordinal);

        // Medidas aplicadas no SIGH (fora do histórico), pela data da medida.
        var sigh = await (
            from m in _db.DisciplinaryMeasures.AsNoTracking()
            join c in _db.DisciplinaryCases.AsNoTracking() on m.DisciplinaryCaseId equals c.Id
            where c.Kind != DisciplinaryCaseKind.Historical
                  && (m.Status == DisciplinaryMeasureStatus.Applied || m.Status == DisciplinaryMeasureStatus.Completed)
            join d in _db.DisciplinaryDecisions.AsNoTracking() on m.DisciplinaryDecisionId equals d.Id into decisions
            from d in decisions.DefaultIfEmpty()
            select new { m.EmployeeId, m.MeasureType, MeasureDate = (DateOnly?)d.MeasureDate, m.AppliedAt, m.EffectiveFrom })
            .ToListAsync(cancellationToken);

        return new HistoryReference(
            employees.GroupBy(e => e.EmployeeNumber).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal),
            units,
            infractions.GroupBy(i => HistoryKeys.Normalize(i.Name)).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal),
            existing,
            sigh.Select(s => new CountedMeasure(s.EmployeeId, s.MeasureDate ?? BusinessCalendar.DateOf(s.AppliedAt ?? s.EffectiveFrom), s.MeasureType)).ToList());
    }

    private async Task<InfractionSeverity> SeverityOfAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.InfractionTypes.AsNoTracking().Where(i => i.Id == id).Select(i => i.DefaultSeverity).FirstAsync(cancellationToken);

    // ---- DTOs ----

    private static HistoryImportCounts Counts(HistoryImportPlan p) =>
        new(p.TotalRows, p.ToImport.Count, p.ToUpdate.Count, p.Existing.Count, p.Rejected.Count, p.Warnings.Count, p.NewInfractionTypes.Count, p.Absent.Count);

    private static HistoryImportSummaryDto Summary(HistoryImportCounts c) =>
        new(c.TotalRows, c.Imported, c.Existing, c.Updated, c.Rejected, c.Warnings, c.NewInfractionTypes, c.Absent);

    private static HistoryRowDto ToDto(PlannedHistoryRow p) => new(
        p.Row.Row, p.Row.EmployeeNumber, p.Employee.FullName, p.Row.Date, p.Row.MeasureType, RecordLabels.Of(p.Row.MeasureType),
        p.Row.InfractionName, p.Infraction is null, p.UnitAtTime.Code, p.UnitAtTime.Name,
        p.Row.PeriodStart, p.Row.PeriodEnd, p.Row.SuspensionDays, p.Row.DaysUnknown);

    private static HistoryImportPreviewDto ToPreview(DisciplinaryHistoryImportBatch batch, HistoryImportPlan plan, IReadOnlyList<string> fileWarnings) => new(
        batch.Id, batch.FileName, batch.FileSha256, batch.ExpiresAt, Summary(Counts(plan)),
        plan.ToImport.Select(ToDto).ToList(),
        plan.Existing.Select(e => ToDto(e.Planned)).ToList(),
        plan.ToUpdate.Select(u => new HistoryUpdateDto(ToDto(u.Planned), u.Existing.CaseId, u.Existing.CaseNumber, u.Changes)).ToList(),
        plan.NewInfractionTypes,
        plan.Warnings,
        plan.Rejected,
        plan.Absent.Select(a => new HistoryAbsentDto(a.CaseId, a.CaseNumber, a.EmployeeNumber, a.Date, RecordLabels.Of(a.MeasureType), a.InfractionName, a.ImportedAt)).ToList(),
        fileWarnings);

    private static string IssuesJson(HistoryImportPlan plan) =>
        JsonSerializer.Serialize(plan.Rejected.Concat(plan.Warnings).Select(i => new { row = i.Row, employeeNumber = i.EmployeeNumber, severity = i.Severity, message = i.Message }));

    private static string NewCode(string name, HashSet<string> used)
    {
        var slug = HistoryKeys.Normalize(name).ToUpperInvariant();
        var baseCode = "HIST_" + (slug.Length > 40 ? slug[..40] : slug);
        var code = baseCode;
        for (var i = 2; !used.Add(code); i++) code = $"{baseCode}_{i}";
        return code;
    }

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;
}
