using System.Security.Cryptography;
using System.Text;
using SIGH.Application.Disciplinary.Records;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.History;

public sealed record HistoryEmployeeRef(Guid Id, string EmployeeNumber, string FullName, Guid ManagementUnitId);
public sealed record HistoryUnitRef(Guid Id, string? Code, string Name);
public sealed record HistoryInfractionRef(Guid Id, string Name);

/// <summary>Registro histórico já gravado (de um lote anterior).</summary>
public sealed record ExistingHistorical(
    Guid CaseId, string CaseNumber, string Key, Guid EmployeeId, string EmployeeNumber, DateOnly Date, DisciplinaryMeasureType MeasureType,
    string InfractionName, Guid? UnitAtTimeId, DateOnly? PeriodStart, DateOnly? PeriodEnd, int? SuspensionDays, DateTimeOffset? ImportedAt);

/// <summary>Medida já aplicada fora deste arquivo (aprovada no SIGH), para recontar advertências e suspensões.</summary>
public sealed record CountedMeasure(Guid EmployeeId, DateOnly Date, DisciplinaryMeasureType MeasureType);

public sealed record HistoryReference(
    IReadOnlyDictionary<string, HistoryEmployeeRef> EmployeesByNumber,
    IReadOnlyList<HistoryUnitRef> Units,
    IReadOnlyDictionary<string, HistoryInfractionRef> InfractionsByKey,
    IReadOnlyDictionary<string, ExistingHistorical> ExistingByKey,
    IReadOnlyList<CountedMeasure> SighMeasures);

public sealed record HistoryIssue(int? Row, string? EmployeeNumber, string Severity, string Message);
public sealed record HistoryFieldChange(string Field, string? OldValue, string? NewValue);

/// <summary>Linha válida com funcionário, gerência da época e motivo resolvidos.</summary>
public sealed record PlannedHistoryRow(HistoryRow Row, HistoryEmployeeRef Employee, HistoryUnitRef UnitAtTime, HistoryInfractionRef? Infraction);

public sealed record PlannedHistoryUpdate(PlannedHistoryRow Planned, ExistingHistorical Existing, IReadOnlyList<HistoryFieldChange> Changes);

public sealed record HistoryImportPlan(
    int TotalRows,
    IReadOnlyList<PlannedHistoryRow> ToImport,
    IReadOnlyList<(PlannedHistoryRow Planned, ExistingHistorical Existing)> Existing,
    IReadOnlyList<PlannedHistoryUpdate> ToUpdate,
    IReadOnlyList<string> NewInfractionTypes,
    IReadOnlyList<HistoryIssue> Warnings,
    IReadOnlyList<HistoryIssue> Rejected,
    IReadOnlyList<ExistingHistorical> Absent,
    string Fingerprint);

/// <summary>
/// Decide, linha a linha, o que a importação do histórico faria (nada é gravado aqui). Regras (HISTORICO.md):
/// - funcionário pela matrícula entre os existentes; não encontrado → rejeitada (importe o SAP antes);
/// - gerência da época pelo nome normalizado das 9 gerências; desconhecida → aviso e gerência atual;
///   diferente da atual → aviso;
/// - motivo pelo nome normalizado; novo → listado (criado na confirmação), com aviso;
/// - chave (matrícula + data + tipo + motivo + início do período) repetida no arquivo → rejeitada;
/// - contadores Nº ADVERTÊNCIA / Nº SUSPENSÃO recalculados por funcionário, por data (histórico já gravado +
///   medidas aprovadas no SIGH + este arquivo) → aviso quando divergem;
/// - mesma chave de um registro gravado: igual → já existente; diferente (gerência, período, dias) → atualizar;
/// - registro gravado que não está no arquivo → "ausente da planilha atual" (nunca apagado).
/// </summary>
public static class HistoryImportPlanner
{
    public const string RejectedSeverity = "Error";
    public const string WarningSeverity = "Warning";

    public const string EmployeeNotFoundMessage = "Funcionário não encontrado — importe a base completa do SAP antes.";

    public static HistoryImportPlan Plan(IReadOnlyList<(int Row, string? EmployeeNumber, HistoryRow? Parsed, string? Error)> rows, HistoryReference reference)
    {
        var warnings = new List<HistoryIssue>();
        var rejected = new List<HistoryIssue>();
        var valid = new List<PlannedHistoryRow>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var newInfractions = new List<string>();
        var newInfractionKeys = new HashSet<string>(StringComparer.Ordinal);
        var unitsByName = reference.Units
            .SelectMany(u => new[] { (Key: HistoryKeys.Normalize(u.Name), Unit: u), (Key: HistoryKeys.Normalize(u.Code), Unit: u) })
            .Where(x => x.Key.Length > 0)
            .GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First().Unit, StringComparer.Ordinal);
        var unitsById = reference.Units.ToDictionary(u => u.Id);

        foreach (var (rowNumber, number, parsed, error) in rows)
        {
            if (parsed is null)
            {
                rejected.Add(new HistoryIssue(rowNumber, number, RejectedSeverity, error!));
                continue;
            }

            if (!reference.EmployeesByNumber.TryGetValue(parsed.EmployeeNumber, out var employee))
            {
                rejected.Add(new HistoryIssue(rowNumber, parsed.EmployeeNumber, RejectedSeverity, EmployeeNotFoundMessage));
                continue;
            }

            if (seen.TryGetValue(parsed.Key, out var firstRow))
            {
                rejected.Add(new HistoryIssue(rowNumber, parsed.EmployeeNumber, RejectedSeverity, $"Linha duplicada dentro do arquivo (igual à linha {firstRow})."));
                continue;
            }
            seen[parsed.Key] = rowNumber;

            var rowWarnings = new List<string>(parsed.Warnings);

            unitsById.TryGetValue(employee.ManagementUnitId, out var current);
            HistoryUnitRef unit;
            if (parsed.UnitText.Length == 0 || !unitsByName.TryGetValue(HistoryKeys.Normalize(parsed.UnitText), out var named))
            {
                unit = current ?? new HistoryUnitRef(employee.ManagementUnitId, null, "?");
                rowWarnings.Add(parsed.UnitText.Length == 0
                    ? $"GERÊNCIA vazia; usada a gerência atual do funcionário ({Describe(unit)})."
                    : $"GERÊNCIA \"{parsed.UnitText}\" não corresponde a nenhuma das gerências; usada a gerência atual do funcionário ({Describe(unit)}).");
            }
            else
            {
                unit = named;
                if (named.Id != employee.ManagementUnitId)
                    rowWarnings.Add($"Gerência da época ({Describe(named)}) diferente da gerência atual do funcionário ({(current is null ? "?" : Describe(current))}).");
            }

            reference.InfractionsByKey.TryGetValue(parsed.InfractionKey, out var infraction);
            if (infraction is null && newInfractionKeys.Add(parsed.InfractionKey))
            {
                newInfractions.Add(parsed.InfractionName);
                rowWarnings.Add($"Motivo novo \"{parsed.InfractionName}\": será criado na confirmação.");
            }

            valid.Add(new PlannedHistoryRow(parsed with { Warnings = rowWarnings }, employee, unit, infraction));
        }

        AddCounterWarnings(valid, reference);

        var toImport = new List<PlannedHistoryRow>();
        var existing = new List<(PlannedHistoryRow, ExistingHistorical)>();
        var toUpdate = new List<PlannedHistoryUpdate>();
        foreach (var planned in valid)
        {
            foreach (var w in planned.Row.Warnings)
                warnings.Add(new HistoryIssue(planned.Row.Row, planned.Row.EmployeeNumber, WarningSeverity, w));

            if (!reference.ExistingByKey.TryGetValue(planned.Row.Key, out var stored))
            {
                toImport.Add(planned);
                continue;
            }

            var changes = Diff(stored, planned, unitsById);
            if (changes.Count == 0) existing.Add((planned, stored));
            else toUpdate.Add(new PlannedHistoryUpdate(planned, stored, changes));
        }

        var present = valid.Select(v => v.Row.Key).ToHashSet(StringComparer.Ordinal);
        var absent = reference.ExistingByKey.Values.Where(e => !present.Contains(e.Key))
            .OrderBy(e => e.EmployeeNumber, StringComparer.Ordinal).ThenBy(e => e.Date).ToList();

        warnings.Sort((a, b) => Nullable.Compare(a.Row, b.Row));
        rejected.Sort((a, b) => Nullable.Compare(a.Row, b.Row));

        return new HistoryImportPlan(rows.Count, toImport, existing, toUpdate, newInfractions, warnings, rejected, absent,
            Fingerprint(toImport, existing, toUpdate, absent, newInfractions));
    }

    /// <summary>
    /// Recontagem por funcionário, por data: medidas aprovadas no SIGH e históricos já gravados que não estão
    /// neste arquivo contam antes das linhas do arquivo na mesma data; o arquivo, na ordem das linhas.
    /// </summary>
    private static void AddCounterWarnings(List<PlannedHistoryRow> valid, HistoryReference reference)
    {
        var fileKeys = valid.Select(v => v.Row.Key).ToHashSet(StringComparer.Ordinal);
        var outside = reference.SighMeasures
            .Concat(reference.ExistingByKey.Values.Where(e => !fileKeys.Contains(e.Key)).Select(e => new CountedMeasure(e.EmployeeId, e.Date, e.MeasureType)))
            .GroupBy(m => m.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var i = 0; i < valid.Count; i++)
        {
            var planned = valid[i];
            var row = planned.Row;
            if (row.SheetWarningCounter is null && row.SheetSuspensionCounter is null) continue;

            var before = outside.TryGetValue(planned.Employee.Id, out var others)
                ? others.Where(m => m.Date <= row.Date).Select(m => m.MeasureType).ToList()
                : [];
            before.AddRange(valid
                .Where(v => v.Employee.Id == planned.Employee.Id
                            && (v.Row.Date < row.Date || (v.Row.Date == row.Date && v.Row.Row <= row.Row)))
                .Select(v => v.Row.MeasureType));

            var warningsCount = before.Count(t => t is DisciplinaryMeasureType.VerbalWarning or DisciplinaryMeasureType.WrittenWarning);
            var suspensionsCount = before.Count(t => t == DisciplinaryMeasureType.Suspension);

            var extra = new List<string>();
            if (row.SheetWarningCounter is { } m && m != warningsCount)
                extra.Add($"Nº ADVERTÊNCIA divergente: planilha {m}, recalculado {warningsCount}.");
            if (row.SheetSuspensionCounter is { } n && n != suspensionsCount)
                extra.Add($"Nº SUSPENSÃO divergente: planilha {n}, recalculado {suspensionsCount}.");
            if (extra.Count > 0)
                valid[i] = planned with { Row = row with { Warnings = [.. row.Warnings, .. extra] } };
        }
    }

    private static List<HistoryFieldChange> Diff(ExistingHistorical stored, PlannedHistoryRow planned, IReadOnlyDictionary<Guid, HistoryUnitRef> units)
    {
        var changes = new List<HistoryFieldChange>();
        if (stored.UnitAtTimeId != planned.UnitAtTime.Id)
            changes.Add(new HistoryFieldChange("Gerência da época",
                stored.UnitAtTimeId is { } u && units.TryGetValue(u, out var old) ? Describe(old) : null, Describe(planned.UnitAtTime)));
        if (stored.PeriodEnd != planned.Row.PeriodEnd)
            changes.Add(new HistoryFieldChange("Fim do período", Format(stored.PeriodEnd), Format(planned.Row.PeriodEnd)));
        if (stored.SuspensionDays != planned.Row.SuspensionDays)
            changes.Add(new HistoryFieldChange("Dias de suspensão", stored.SuspensionDays?.ToString() ?? "não informado", planned.Row.SuspensionDays?.ToString() ?? "não informado"));
        return changes;
    }

    public static string Describe(HistoryUnitRef unit) => unit.Code is null ? unit.Name : $"{unit.Code} – {unit.Name}";

    private static string? Format(DateOnly? date) => date?.ToString("dd/MM/yyyy");

    private static string Fingerprint(
        IEnumerable<PlannedHistoryRow> toImport, IEnumerable<(PlannedHistoryRow Planned, ExistingHistorical Existing)> existing,
        IEnumerable<PlannedHistoryUpdate> toUpdate, IEnumerable<ExistingHistorical> absent, IEnumerable<string> newInfractions)
    {
        var lines = new List<string>();
        lines.AddRange(toImport.Select(p => $"I|{p.Row.Key}|{p.UnitAtTime.Id}|{p.Row.PeriodEnd}|{p.Row.SuspensionDays}|{p.Infraction?.Id}"));
        lines.AddRange(existing.Select(e => $"E|{e.Existing.CaseId}"));
        lines.AddRange(toUpdate.Select(u => $"U|{u.Existing.CaseId}|{u.Planned.UnitAtTime.Id}|{u.Planned.Row.PeriodEnd}|{u.Planned.Row.SuspensionDays}"));
        lines.AddRange(absent.Select(a => $"A|{a.CaseId}"));
        lines.AddRange(newInfractions.Select(n => $"N|{HistoryKeys.Normalize(n)}"));
        lines.Sort(StringComparer.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines)))).ToLowerInvariant();
    }

    public static string LabelOf(DisciplinaryMeasureType type) => RecordLabels.Of(type);
}
