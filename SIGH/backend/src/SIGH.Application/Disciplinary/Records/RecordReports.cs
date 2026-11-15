using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SIGH.Application.Disciplinary.Reports.Utils;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Records;

/// <summary>
/// Relatórios sobre as medidas (página Relatórios e Painel). Tudo parte de <see cref="FilteredAsync"/>,
/// que já aplica o escopo por gerência: o gestor só conta as medidas das gerências dele.
/// </summary>
public sealed partial class RecordQueries
{
    /// <summary>Limite de linhas do CSV (o RH exporta recortes; acima disso, filtre).</summary>
    public const int MaxExportRows = 10_000;

    private const string CsvSeparator = ";";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<RecordStatsDto> GetStatsAsync(GetRecordsQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await LoadLightAsync(await FilteredAsync(query, cancellationToken), cancellationToken);
        var total = rows.Count;

        var byStatus = rows.GroupBy(StatusOf)
            .Select(g => Item((int)g.Key, RecordLabels.Of(g.Key), g.Count(), total))
            .OrderByDescending(i => i.Count).ThenBy(i => i.Value).ToList();
        var byType = rows.GroupBy(r => r.MeasureType ?? DisciplinaryMeasureType.Other)
            .Select(g => Item((int)g.Key, RecordLabels.Of(g.Key), g.Count(), total))
            .OrderByDescending(i => i.Count).ThenBy(i => i.Value).ToList();

        var approvalDays = rows.Select(DaysToApproval).Where(d => d is not null).Select(d => d!.Value).ToList();
        var notCancelled = rows.Where(r => StatusOf(r) != RecordStatus.Cancelled).ToList();

        return new RecordStatsDto(
            total,
            byStatus,
            byType,
            rows.Select(r => r.EmployeeId).Distinct().Count(),
            notCancelled.Sum(r => SuspensionDaysOf(r) ?? 0),
            approvalDays.Count == 0 ? null : Math.Round((decimal)approvalDays.Average(), 1));
    }

    public async Task<byte[]> ExportCsvAsync(GetRecordsQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await (await FilteredAsync(query, cancellationToken))
            .OrderByDescending(r => r.OpenedAt)
            .Take(MaxExportRows)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.Append('﻿'); // BOM: o Excel em pt-BR abre com acentos corretos.
        csv.AppendLine(string.Join(CsvSeparator,
            "Número", "Matrícula", "Funcionário", "Gerência", "Setor", "Medida", "Motivo", "Data da medida",
            "Início da suspensão", "Fim da suspensão", "Dias de suspensão", "Situação", "Origem",
            "Registrada por", "Registrada em", "Aplicado por", "Aprovada em", "Dias até a aprovação"));

        foreach (var summary in rows.Select(ToSummary))
        {
            var appliedBy = summary.Origin == RecordOrigin.Historical ? "Histórico importado" : summary.AppliedByName;
            csv.AppendLine(string.Join(CsvSeparator,
                Field(summary.CaseNumber),
                Field(summary.EmployeeNumber),
                Field(summary.EmployeeName),
                Field(summary.ManagementUnitCode is null ? null : $"{summary.ManagementUnitCode} – {summary.ManagementUnitName}"),
                Field(summary.DepartmentName),
                Field(summary.MeasureTypeLabel),
                Field(summary.InfractionTypeName),
                Field(Date(summary.MeasureDate)),
                Field(Date(summary.SuspensionStart)),
                Field(Date(summary.SuspensionEnd)),
                Field(summary.SuspensionDaysUnknown ? "Não informado" : summary.SuspensionDays?.ToString(PtBr)),
                Field(summary.StatusLabel),
                Field(summary.OriginLabel),
                Field(summary.RegisteredByName),
                Field(Instant(summary.RegisteredAt)),
                Field(appliedBy),
                Field(summary.ApprovedAt is { } approved ? Instant(approved) : null),
                Field(summary.DaysToApproval?.ToString(PtBr))));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<RecordOverviewDto> GetOverviewAsync(OverviewPeriod period, CancellationToken cancellationToken = default)
    {
        var today = BusinessCalendar.Today(_clock.UtcNow);
        var (from, to, previousFrom, previousTo) = RangeOf(period, today);

        // Evolução mensal: os meses do período, com no mínimo os últimos 6 (o gráfico precisa de pontos).
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var seriesFrom = Min(new DateOnly(from.Year, from.Month, 1), monthStart.AddMonths(-5));
        var loadFrom = Min(seriesFrom, previousFrom);

        var rows = (await LoadLightAsync(await FilteredAsync(new GetRecordsQuery(From: loadFrom, To: to), cancellationToken), cancellationToken))
            .Where(r => StatusOf(r) != RecordStatus.Cancelled)
            .Select(r => (Row: r, Date: DateOf(r)))
            .Where(x => x.Date is not null)
            .Select(x => (x.Row, Date: x.Date!.Value))
            .ToList();

        List<Row> In(DateOnly start, DateOnly end) => rows.Where(x => x.Date >= start && x.Date <= end).Select(x => x.Row).ToList();
        var current = In(from, to);
        var previous = In(previousFrom, previousTo);

        OverviewKpiDto Kpi(Func<List<Row>, int> measure) => new(measure(current), measure(previous));
        static List<Row> Suspensions(List<Row> list) => list.Where(r => r.MeasureType == DisciplinaryMeasureType.Suspension).ToList();

        var months = new List<MonthPointDto>();
        for (var month = seriesFrom; month <= monthStart; month = month.AddMonths(1))
        {
            var end = month.AddMonths(1).AddDays(-1);
            months.Add(new MonthPointDto(month.Year, month.Month, rows.Count(x => x.Date >= month && x.Date <= end)));
        }

        var visible = await VisibleRecordsAsync(cancellationToken);
        var pending = await visible.CountAsync(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision
                                                    && c.Decisions.Any(d => d.Status == DecisionStatus.PendingApproval), cancellationToken);

        return new RecordOverviewDto(
            period, from, to, previousFrom, previousTo,
            Kpi(list => list.Count),
            pending,
            Kpi(list => Suspensions(list).Count),
            Kpi(list => Suspensions(list).Sum(r => SuspensionDaysOf(r) ?? 0)),
            Kpi(list => list.Select(r => r.EmployeeId).Distinct().Count()),
            months,
            await RecentActivitiesAsync(8, cancellationToken));
    }

    /// <summary>Últimas medidas registradas, aprovadas e rejeitadas (no escopo do usuário).</summary>
    private async Task<IReadOnlyList<RecordActivityDto>> RecentActivitiesAsync(int take, CancellationToken cancellationToken)
    {
        var records = (await VisibleRecordsAsync(cancellationToken)).Where(c => c.Kind == DisciplinaryCaseKind.Record);

        var registered = await records.OrderByDescending(c => c.OpenedAt).Take(take)
            .Select(c => new { c.Id, At = c.OpenedAt, Actor = (Guid?)c.OpenedByUserId })
            .ToListAsync(cancellationToken);
        var approved = await records.SelectMany(c => c.Decisions
                .Where(d => d.Status == DecisionStatus.Approved && d.ApprovedAt != null)
                .Select(d => new { c.Id, At = d.ApprovedAt!.Value, Actor = d.ApprovedByUserId }))
            .OrderByDescending(x => x.At).Take(take).ToListAsync(cancellationToken);
        var rejected = await records.SelectMany(c => c.Decisions
                .Where(d => d.Status == DecisionStatus.Rejected && d.RejectedAt != null)
                .Select(d => new { c.Id, At = d.RejectedAt!.Value, Actor = d.RejectedByUserId }))
            .OrderByDescending(x => x.At).Take(take).ToListAsync(cancellationToken);

        var events = registered.Select(x => (Kind: RecordActivityKind.Registered, x.Id, x.At, x.Actor))
            .Concat(approved.Select(x => (Kind: RecordActivityKind.Approved, x.Id, x.At, x.Actor)))
            .Concat(rejected.Select(x => (Kind: RecordActivityKind.Rejected, x.Id, x.At, x.Actor)))
            .OrderByDescending(e => e.At).Take(take).ToList();
        if (events.Count == 0)
            return [];

        var ids = events.Select(e => e.Id).Distinct().ToList();
        var summaries = (await Project(records.Where(c => ids.Contains(c.Id))).ToListAsync(cancellationToken))
            .ToDictionary(r => r.Id);
        var actorIds = events.Where(e => e.Actor is not null).Select(e => e.Actor!.Value).Distinct().ToList();
        var actors = await _db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return events.Where(e => summaries.ContainsKey(e.Id)).Select(e =>
        {
            var row = summaries[e.Id];
            var type = row.MeasureType ?? DisciplinaryMeasureType.Other;
            return new RecordActivityDto(e.Kind, e.Id, row.CaseNumber, row.EmployeeName, type, RecordLabels.Of(type),
                e.Actor is { } actor && actors.TryGetValue(actor, out var name) ? name : null, e.At);
        }).ToList();
    }

    /// <summary>Período escolhido e o anterior de mesmo tamanho (no ano atual: o mesmo trecho do ano passado).</summary>
    public static (DateOnly From, DateOnly To, DateOnly PreviousFrom, DateOnly PreviousTo) RangeOf(OverviewPeriod period, DateOnly today)
    {
        switch (period)
        {
            case OverviewPeriod.Last90Days:
                return (today.AddDays(-89), today, today.AddDays(-179), today.AddDays(-90));
            case OverviewPeriod.Last12Months:
            {
                var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
                return (from, today, from.AddMonths(-12), from.AddDays(-1));
            }
            case OverviewPeriod.CurrentYear:
            {
                var from = new DateOnly(today.Year, 1, 1);
                return (from, today, from.AddYears(-1), today.AddYears(-1));
            }
            default:
                return (today.AddDays(-29), today, today.AddDays(-59), today.AddDays(-30));
        }
    }

    /// <summary>Só os campos usados nas contagens (a projeção completa traz nomes e subconsultas).</summary>
    private static Task<List<Row>> LoadLightAsync(IQueryable<Row> rows, CancellationToken cancellationToken) =>
        rows.Select(r => new Row
        {
            Id = r.Id,
            Kind = r.Kind,
            CaseStatus = r.CaseStatus,
            HasPendingDecision = r.HasPendingDecision,
            MeasureType = r.MeasureType,
            MeasureDate = r.MeasureDate,
            FirstOccurrence = r.FirstOccurrence,
            EmployeeId = r.EmployeeId,
            SuspensionStart = r.SuspensionStart,
            SuspensionEnd = r.SuspensionEnd,
            MeasureEffectiveFrom = r.MeasureEffectiveFrom,
            MeasureEffectiveUntil = r.MeasureEffectiveUntil,
            MeasureSuspensionDays = r.MeasureSuspensionDays,
            OpenedAt = r.OpenedAt,
            ApprovedAt = r.ApprovedAt,
        }).ToListAsync(cancellationToken);

    /// <summary>Data de referência da medida: a data da medida; no histórico, a da ocorrência.</summary>
    private static DateOnly? DateOf(Row row) =>
        row.Kind != DisciplinaryCaseKind.Historical && row.MeasureDate is { } date
            ? date
            : row.FirstOccurrence is { } first ? BusinessCalendar.DateOf(first) : row.MeasureDate;

    private static int? SuspensionDaysOf(Row row)
    {
        if (row.MeasureType != DisciplinaryMeasureType.Suspension)
            return null;
        var (start, end) = PeriodOf(row);
        return row.MeasureSuspensionDays ?? (start is { } s && end is { } e ? e.DayNumber - s.DayNumber + 1 : null);
    }

    private static CountItemDto Item(int value, string label, int count, int total) =>
        new(value, label, count, total == 0 ? 0 : Math.Round(count * 100m / total, 1));

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static string Field(string? value) => CsvSanitizer.SanitizeField(value, CsvSeparator);

    private static string? Date(DateOnly? date) => date?.ToString("dd/MM/yyyy", PtBr);

    private static string Instant(DateTimeOffset instant) =>
        instant.ToOffset(BusinessCalendar.Offset).ToString("dd/MM/yyyy HH:mm", PtBr);
}
