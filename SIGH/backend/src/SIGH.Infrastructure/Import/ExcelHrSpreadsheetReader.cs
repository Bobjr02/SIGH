using System.Globalization;
using ExcelDataReader;
using SIGH.Application.Employees.Import;

namespace SIGH.Infrastructure.Import;

/// <summary>
/// Leitura do xlsx exportado do SAP com ExcelDataReader (MIT), em streaming. Só as colunas da lista
/// fechada (<see cref="HrImportColumns"/>) são convertidas para texto; as demais nem são lidas para
/// objetos nossos, e nenhuma exceção ou mensagem inclui conteúdo de célula.
/// </summary>
public sealed class ExcelHrSpreadsheetReader : IHrSpreadsheetReader, ISpreadsheetTableReader
{
    /// <summary>O cabeçalho é procurado nas primeiras linhas (a exportação pode ter título acima).</summary>
    private const int MaxHeaderSearchRows = 20;

    // O ExcelDataReader precisa das code pages legadas (ex.: 1252), que no .NET moderno ficam num
    // provider separado. Antes isso só funcionava porque o SqlClient o registrava indiretamente.
    static ExcelHrSpreadsheetReader() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    public HrSheet Read(Stream xlsx)
    {
        var columns = HrImportColumns.All
            .Select(c => new SheetColumn(c.Field.ToString(), c.Header, c.Required, c.Label))
            .ToList();
        var table = Read(xlsx, columns, HrField.EmployeeNumber.ToString());
        var rows = table.Rows
            .Select(r => new HrRawRow(r.RowNumber, r.Cells.ToDictionary(kv => Enum.Parse<HrField>(kv.Key), kv => kv.Value)))
            .ToList();
        return new HrSheet(rows, table.Warnings);
    }

    public SheetTable Read(Stream xlsx, IReadOnlyList<SheetColumn> columns, string anchorKey)
    {
        IExcelDataReader reader;
        try
        {
            reader = ExcelReaderFactory.CreateOpenXmlReader(xlsx);
        }
        catch (Exception)
        {
            throw new HrSpreadsheetException("O arquivo não é uma planilha .xlsx válida.");
        }

        using (reader)
        {
            Dictionary<string, int>? map = null;
            var warnings = new List<string>();
            var rows = new List<SheetRow>();
            var excelRow = 0;
            var bestMissing = new List<string>();

            try
            {
                while (reader.Read())
                {
                    excelRow++;

                    if (map is null)
                    {
                        if (excelRow > MaxHeaderSearchRows) break;
                        var (candidate, missing, duplicated, missingOptional) = MapHeader(reader, columns, anchorKey);
                        if (candidate is null)
                        {
                            if (bestMissing.Count == 0 || (missing.Count > 0 && missing.Count < bestMissing.Count)) bestMissing = missing;
                            continue;
                        }
                        map = candidate;
                        // Coluna opcional ausente nunca passa em silêncio: o campo ficaria vazio para todos.
                        warnings.AddRange(missingOptional.Select(c =>
                            $"Coluna \"{c.Header}\" não encontrada; o campo {c.Label} ficará vazio para todos."));
                        warnings.AddRange(duplicated.Select(d =>
                            $"O cabeçalho \"{d}\" aparece mais de uma vez; foi usada a primeira ocorrência."));
                        continue;
                    }

                    var cells = new Dictionary<string, HrCell>(map.Count);
                    var empty = true;
                    foreach (var (key, index) in map)
                    {
                        var cell = ToCell(index < reader.FieldCount ? reader.GetValue(index) : null);
                        if (cell.Text.Length > 0) empty = false;
                        cells[key] = cell;
                    }
                    if (!empty) rows.Add(new SheetRow(excelRow, cells));
                }
            }
            catch (HrSpreadsheetException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new HrSpreadsheetException("Não foi possível ler a planilha. Verifique se o arquivo é uma planilha .xlsx válida.");
            }

            if (map is null)
            {
                var names = bestMissing.Count > 0
                    ? bestMissing
                    : columns.Where(c => c.Required).Select(c => c.Header).ToList();
                throw new HrSpreadsheetException(
                    $"Coluna(s) obrigatória(s) não encontrada(s) no cabeçalho: {string.Join(", ", names.Select(n => $"\"{n}\""))}.");
            }

            return new SheetTable(rows, warnings);
        }
    }

    /// <summary>
    /// Tenta usar a linha atual como cabeçalho. Só é cabeçalho se tiver a coluna âncora; nesse caso todas
    /// as obrigatórias precisam estar presentes. Cabeçalhos duplicados ("Estado" duas vezes) são tolerados.
    /// Colunas com ReadValues = false são conferidas mas nunca lidas.
    /// </summary>
    private static (Dictionary<string, int>? Map, List<string> Missing, List<string> Duplicated, List<SheetColumn> MissingOptional) MapHeader(
        IExcelDataReader reader, IReadOnlyList<SheetColumn> columns, string anchorKey)
    {
        var positions = new Dictionary<string, int>();
        var duplicatedKeys = new HashSet<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var key = HrImportColumns.Normalize(reader.GetValue(i)?.ToString());
            if (key.Length == 0) continue;
            if (!positions.TryAdd(key, i)) duplicatedKeys.Add(key);
        }

        var anchorHeader = HrImportColumns.Normalize(columns.First(c => c.Key == anchorKey).Header);
        if (!positions.ContainsKey(anchorHeader))
            return (null, new List<string>(), new List<string>(), new List<SheetColumn>());

        var map = new Dictionary<string, int>();
        var missing = new List<string>();
        var duplicated = new List<string>();
        var missingOptional = new List<SheetColumn>();
        foreach (var column in columns)
        {
            var key = HrImportColumns.Normalize(column.Header);
            if (!positions.ContainsKey(key))
            {
                if (column.Required) missing.Add(column.Header);
                else missingOptional.Add(column);
                continue;
            }
            if (column.ReadValues)
                map[column.Key] = positions[key];
            if (duplicatedKeys.Contains(key)) duplicated.Add(column.Header);
        }

        return missing.Count > 0 ? (null, missing, duplicated, missingOptional) : (map, missing, duplicated, missingOptional);
    }

    private static HrCell ToCell(object? value) => value switch
    {
        null => new HrCell(string.Empty, false),
        string s => new HrCell(s.Trim(), false),
        double d => new HrCell(FormatNumber(d), true),
        float f => new HrCell(FormatNumber(f), true),
        int i => new HrCell(i.ToString(CultureInfo.InvariantCulture), true),
        long l => new HrCell(l.ToString(CultureInfo.InvariantCulture), true),
        decimal m => new HrCell(m.ToString(CultureInfo.InvariantCulture), true),
        DateTime dt => new HrCell(dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), false),
        bool b => new HrCell(b ? "true" : "false", false),
        _ => new HrCell(Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty, false)
    };

    private static string FormatNumber(double d) =>
        Math.Abs(d % 1) < double.Epsilon && Math.Abs(d) < 1e15
            ? ((long)d).ToString(CultureInfo.InvariantCulture)
            : d.ToString(CultureInfo.InvariantCulture);
}
