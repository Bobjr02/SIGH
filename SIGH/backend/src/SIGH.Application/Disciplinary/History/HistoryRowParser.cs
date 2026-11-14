using System.Globalization;
using System.Text.RegularExpressions;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.History;

/// <summary>
/// Os 14 cabeçalhos da planilha de penalidades (LEIAME, seção 3), comparados pelo nome normalizado. Todos
/// precisam existir; NOME DO FUNCIONÁRIO, CARGO e SETOR (cópias do cadastro da época) nunca são lidos.
/// Nº ADVERTÊNCIA e Nº SUSPENSÃO não são importados: só servem para avisar quando divergem da recontagem.
/// </summary>
public static class HistoryColumns
{
    public const string EmployeeNumber = "Matricula";
    public const string Date = "Data";
    public const string TypeText = "TipoInfracao";
    public const string Reason = "Motivo";
    public const string Days = "Dias";
    public const string Period = "Periodo";
    public const string EndDate = "DataFim";
    public const string Unit = "Gerencia";
    public const string Infraction = "Infracao";
    public const string WarningCounter = "NumAdvertencia";
    public const string SuspensionCounter = "NumSuspensao";

    public static readonly IReadOnlyList<SheetColumn> All =
    [
        new(EmployeeNumber, "MATRÍCULA", true, "matrícula"),
        new(Date, "DATA", true, "data"),
        new(TypeText, "TIPO DA INFRAÇÃO", true, "tipo da infração"),
        new(Reason, "MOTIVO DA INFRAÇÃO", true, "motivo"),
        new(Days, "DIAS DE SUSPENSÃO", true, "dias de suspensão"),
        new(Period, "PERÍODO DA SUSPENSÃO", true, "período da suspensão"),
        new("Nome", "NOME DO FUNCIONÁRIO", true, "nome", ReadValues: false),
        new("Cargo", "CARGO", true, "cargo", ReadValues: false),
        new("Setor", "SETOR", true, "setor", ReadValues: false),
        new(EndDate, "DATA FIM DA SUSPENSÃO", true, "data fim da suspensão"),
        new(Unit, "GERÊNCIA", true, "gerência"),
        new(Infraction, "INFRAÇÃO", true, "infração"),
        new(WarningCounter, "Nº ADVERTÊNCIA", true, "nº advertência"),
        new(SuspensionCounter, "Nº SUSPENSÃO", true, "nº suspensão"),
    ];
}

/// <summary>Linha válida da planilha, já interpretada.</summary>
public sealed record HistoryRow(
    int Row,
    string EmployeeNumber,
    DateOnly Date,
    DisciplinaryMeasureType MeasureType,
    string TypeText,
    string InfractionName,
    string InfractionKey,
    string UnitText,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    int? SuspensionDays,
    int? SheetWarningCounter,
    int? SheetSuspensionCounter,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Chave de idempotência: matrícula + data + tipo de medida + motivo normalizado + início do período.</summary>
    public string Key => HistoryKeys.Of(EmployeeNumber, Date, MeasureType, InfractionKey, PeriodStart);

    public bool DaysUnknown => MeasureType == DisciplinaryMeasureType.Suspension && SuspensionDays is null;
}

public static class HistoryKeys
{
    public static string Of(string employeeNumber, DateOnly date, DisciplinaryMeasureType type, string infractionKey, DateOnly? periodStart) =>
        $"{employeeNumber}|{date:yyyy-MM-dd}|{(int)type}|{infractionKey}|{(periodStart is { } p ? p.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty)}";

    /// <summary>Nome comparável: sem acento, sem diferença de maiúsculas, só letras e dígitos.</summary>
    public static string Normalize(string? text) => HrImportColumns.Normalize(text);
}

/// <summary>
/// Interpreta uma linha da planilha conforme HISTORICO.md: tipo de medida (INFRAÇÃO / TIPO DA INFRAÇÃO), datas
/// (data do Excel ou texto dd/MM/yyyy), período da suspensão (PERÍODO → DATA..DATA FIM → sem período) e os
/// avisos de dias divergentes, de suspensão sem período e de advertência com dias.
/// </summary>
public static class HistoryRowParser
{
    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    // "dd/MM/yyyy À dd/MM/yyyy", aceitando À, à, A, a ou até entre as datas.
    private static readonly Regex PeriodPattern = new(
        @"^\s*(\d{1,2}/\d{1,2}/\d{4})\s*(?:À|à|A|a|até|ATÉ|Até|ate|ATE)\s*(\d{1,2}/\d{1,2}/\d{4})\s*$",
        RegexOptions.CultureInvariant);

    public static (HistoryRow? Row, string? Error) Parse(SheetRow raw)
    {
        var number = raw.Get(HistoryColumns.EmployeeNumber).Text.Trim();
        if (number.Length == 0 || !number.All(char.IsAsciiDigit))
            return (null, "MATRÍCULA vazia ou inválida.");

        if (ParseDate(raw.Get(HistoryColumns.Date)) is not { } date)
            return (null, "DATA vazia ou inválida (use uma data do Excel ou dd/MM/aaaa).");

        var typeText = raw.Get(HistoryColumns.TypeText).Text.Trim();
        var infraction = raw.Get(HistoryColumns.Infraction).Text.Trim();
        if (MeasureTypeOf(infraction, typeText) is not { } type)
            return (null, "Tipo de medida não reconhecido em INFRAÇÃO / TIPO DA INFRAÇÃO.");

        var reason = raw.Get(HistoryColumns.Reason).Text.Trim();
        var reasonKey = HistoryKeys.Normalize(reason);
        if (reasonKey.Length == 0)
            return (null, "MOTIVO DA INFRAÇÃO vazio.");

        var warnings = new List<string>();
        var daysCell = raw.Get(HistoryColumns.Days);
        var periodText = raw.Get(HistoryColumns.Period).Text.Trim();
        var endCell = raw.Get(HistoryColumns.EndDate);
        int? sheetDays = ParseInt(daysCell.Text) is > 0 and var d ? d : null;

        DateOnly? start = null, end = null;
        int? days = null;

        if (type == DisciplinaryMeasureType.Suspension)
        {
            // 1. PERÍODO DA SUSPENSÃO
            if (periodText.Length > 0)
            {
                var match = PeriodPattern.Match(periodText);
                if (match.Success && ParseDateText(match.Groups[1].Value) is { } ps && ParseDateText(match.Groups[2].Value) is { } pe)
                {
                    if (pe < ps)
                        return (null, "PERÍODO DA SUSPENSÃO com fim anterior ao início.");
                    (start, end) = (ps, pe);
                }
                else
                {
                    warnings.Add($"PERÍODO DA SUSPENSÃO ilegível (\"{periodText}\"); ignorado.");
                }
            }

            // 2. DATA até DATA FIM DA SUSPENSÃO
            if (start is null && endCell.Text.Length > 0)
            {
                if (ParseDate(endCell) is { } fe)
                {
                    if (fe < date)
                        return (null, "DATA FIM DA SUSPENSÃO anterior à DATA.");
                    (start, end) = (date, fe);
                }
                else
                {
                    warnings.Add("DATA FIM DA SUSPENSÃO inválida; ignorada.");
                }
            }

            if (start is { } s && end is { } e)
            {
                days = e.DayNumber - s.DayNumber + 1;
                if (sheetDays is { } informed && informed != days)
                    warnings.Add($"DIAS DE SUSPENSÃO ({informed}) diverge do período ({days} dias); vale o período.");
            }
            else if (sheetDays is { } informed)
            {
                days = informed;
                warnings.Add($"Suspensão sem período: usados os {informed} DIAS DE SUSPENSÃO da planilha.");
            }
            else
            {
                // 3. Sem período: dias DESCONHECIDOS (nulo, nunca zero).
                warnings.Add("Suspensão sem período e sem dias: importada com dias desconhecidos.");
            }
        }
        else if (sheetDays is not null || periodText.Length > 0 || endCell.Text.Length > 0)
        {
            warnings.Add($"{RecordLabels.Of(type)} com dias ou período de suspensão preenchidos; os dias foram ignorados.");
        }

        return (new HistoryRow(raw.RowNumber, number, date, type, typeText, reason, reasonKey,
            raw.Get(HistoryColumns.Unit).Text.Trim(), start, end, days,
            ParseInt(raw.Get(HistoryColumns.WarningCounter).Text), ParseInt(raw.Get(HistoryColumns.SuspensionCounter).Text),
            warnings), null);
    }

    /// <summary>
    /// "ADVERTÊNCIA" → advertência escrita (verbal se TIPO contiver "VERBAL"); "SUSPENSÃO" → suspensão
    /// (o "1" de "SUSPENSÃO 1" é rótulo); "JUSTA CAUSA" em INFRAÇÃO ou TIPO → demissão por justa causa.
    /// </summary>
    public static DisciplinaryMeasureType? MeasureTypeOf(string infraction, string typeText)
    {
        var inf = HistoryKeys.Normalize(infraction);
        var tipo = HistoryKeys.Normalize(typeText);
        if (inf.Contains("justacausa") || tipo.Contains("justacausa"))
            return DisciplinaryMeasureType.JustCauseTermination;

        var source = inf.Length > 0 ? inf : tipo;
        if (source.Contains("suspens"))
            return DisciplinaryMeasureType.Suspension;
        if (source.Contains("advertencia"))
            return tipo.Contains("verbal") ? DisciplinaryMeasureType.VerbalWarning : DisciplinaryMeasureType.WrittenWarning;
        return null;
    }

    /// <summary>Data do Excel (o leitor entrega dd/MM/yyyy), número de série do Excel ou texto dd/MM/yyyy.</summary>
    public static DateOnly? ParseDate(HrCell cell)
    {
        var text = cell.Text.Trim();
        if (text.Length == 0) return null;
        if (cell.WasNumber && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is >= 1 and < 2958466)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        return ParseDateText(text);
    }

    private static DateOnly? ParseDateText(string text) =>
        DateOnly.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;

    private static int? ParseInt(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && Math.Abs(value % 1) < 1e-9
            ? (int)value
            : null;
}
