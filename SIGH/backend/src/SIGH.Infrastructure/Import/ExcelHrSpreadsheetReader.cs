using System.Globalization;
using ExcelDataReader;
using SIGH.Application.Employees.Import;

namespace SIGH.Infrastructure.Import;

/// <summary>
/// Leitura do xlsx exportado do SAP com ExcelDataReader (MIT), em streaming. Só as colunas da lista
/// fechada (<see cref="HrImportColumns"/>) são convertidas para texto; as demais nem são lidas para
/// objetos nossos, e nenhuma exceção ou mensagem inclui conteúdo de célula.
/// </summary>
public sealed class ExcelHrSpreadsheetReader : IHrSpreadsheetReader
{
    /// <summary>O cabeçalho é procurado nas primeiras linhas (a exportação pode ter título acima).</summary>
    private const int MaxHeaderSearchRows = 20;

    // O ExcelDataReader precisa das code pages legadas (ex.: 1252), que no .NET moderno ficam num
    // provider separado. Antes isso só funcionava porque o SqlClient o registrava indiretamente.
    static ExcelHrSpreadsheetReader() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    public HrSheet Read(Stream xlsx)
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
            Dictionary<HrField, int>? map = null;
            var warnings = new List<string>();
            var rows = new List<HrRawRow>();
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
                        var (candidate, missing, duplicated) = MapHeader(reader);
                        if (candidate is null)
                        {
                            if (bestMissing.Count == 0 || (missing.Count > 0 && missing.Count < bestMissing.Count)) bestMissing = missing;
                            continue;
                        }
                        map = candidate;
                        warnings.AddRange(duplicated.Select(d =>
                            $"O cabeçalho \"{d}\" aparece mais de uma vez; foi usada a primeira ocorrência."));
                        continue;
                    }

                    var cells = new Dictionary<HrField, HrCell>(map.Count);
                    var empty = true;
                    foreach (var (field, index) in map)
                    {
                        var cell = ToCell(index < reader.FieldCount ? reader.GetValue(index) : null);
                        if (cell.Text.Length > 0) empty = false;
                        cells[field] = cell;
                    }
                    if (!empty) rows.Add(new HrRawRow(excelRow, cells));
                }
            }
            catch (HrSpreadsheetException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new HrSpreadsheetException("Não foi possível ler a planilha. Verifique se o arquivo é a exportação .xlsx do SAP.");
            }

            if (map is null)
            {
                var names = bestMissing.Count > 0
                    ? bestMissing
                    : HrImportColumns.All.Where(c => c.Required).Select(c => c.Headers[0]).ToList();
                throw new HrSpreadsheetException(
                    $"Coluna(s) obrigatória(s) não encontrada(s) no cabeçalho: {string.Join(", ", names.Select(n => $"\"{n}\""))}.");
            }

            return new HrSheet(rows, warnings);
        }
    }

    /// <summary>
    /// Tenta usar a linha atual como cabeçalho. Só é cabeçalho se tiver "Matrícula"; nesse caso todas
    /// as obrigatórias precisam estar presentes. Cabeçalhos duplicados ("Estado" duas vezes) são tolerados.
    /// </summary>
    private static (Dictionary<HrField, int>? Map, List<string> Missing, List<string> Duplicated) MapHeader(IExcelDataReader reader)
    {
        var positions = new Dictionary<string, int>();
        var duplicatedKeys = new HashSet<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var key = HrImportColumns.Normalize(reader.GetValue(i)?.ToString());
            if (key.Length == 0) continue;
            if (!positions.TryAdd(key, i)) duplicatedKeys.Add(key);
        }

        var employeeNumberKeys = HrImportColumns.All.First(c => c.Field == HrField.EmployeeNumber).Headers.Select(HrImportColumns.Normalize);
        if (!employeeNumberKeys.Any(positions.ContainsKey))
            return (null, new List<string>(), new List<string>());

        var map = new Dictionary<HrField, int>();
        var missing = new List<string>();
        var duplicated = new List<string>();
        foreach (var column in HrImportColumns.All)
        {
            var key = column.Headers.Select(HrImportColumns.Normalize).FirstOrDefault(positions.ContainsKey);
            if (key is null)
            {
                if (column.Required) missing.Add(column.Headers[0]);
                continue;
            }
            map[column.Field] = positions[key];
            if (duplicatedKeys.Contains(key)) duplicated.Add(column.Headers[0]);
        }

        return missing.Count > 0 ? (null, missing, duplicated) : (map, missing, duplicated);
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
