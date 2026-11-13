using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>
/// Gera, em memória, planilhas .xlsx no formato da exportação do SAP, com dados SINTÉTICOS
/// (nada é versionado). Inclui colunas fora da lista fechada com marcadores ("FICTICIA",
/// "FICTICIO", "00000-0") para provar que elas nunca chegam ao banco nem aos logs.
/// </summary>
public sealed class HrXlsxBuilder
{
    public static readonly string[] SensitiveMarkers = ["FICTICIA", "FICTICIO", "00000-0"];

    public sealed record Row(
        object EmployeeNumber,
        string Name,
        object Cpf,
        string Status = "Ativo",
        string Admission = "01/02/2020",
        string Termination = "",
        string JobCode = "50001234",
        string JobName = "Operador de Máquinas",
        string DeptCode = "60001000",
        string DeptName = "Moagem",
        string ManagementUnit = "USGA131",
        string Group = "Indústria",
        string Team = "A");

    private readonly List<string> _headers;
    private readonly int _extraColumns;
    private readonly List<Row> _rows = new();
    private readonly HashSet<string> _omit = new(StringComparer.Ordinal);

    /// <param name="extraColumns">Colunas extras (fora da lista) para chegar perto das 72 da exportação real.</param>
    public HrXlsxBuilder(int extraColumns = 8)
    {
        _extraColumns = extraColumns;
        _headers =
        [
            "Matrícula", "Nome", "Nome da mãe", "CPF", "Estado", "Data admissão", "Data demissão", "Status ocupação",
            "Cargo", "Descr.Cargo", "Uni.Org", "Descr.Unid.Org.", "Chave Organização", "Grupo", "Turma",
            "Endereço", "CEP", "Estado", "Telefone", "PIS", "Conta bancária"
        ];
    }

    public HrXlsxBuilder Add(Row row) { _rows.Add(row); return this; }

    public HrXlsxBuilder AddRange(IEnumerable<Row> rows) { _rows.AddRange(rows); return this; }

    /// <summary>Remove uma coluna do cabeçalho (para testar coluna obrigatória ausente).</summary>
    public HrXlsxBuilder Without(string header) { _omit.Add(header); return this; }

    public byte[] Build()
    {
        var headers = _headers.Where(h => !_omit.Contains(h)).ToList();
        for (var i = 0; i < _extraColumns; i++) headers.Add($"Campo SAP {i + 1:00}");

        using var buffer = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(buffer, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var sheetPart = workbook.AddNewPart<WorksheetPart>();
            var data = new SheetData();
            sheetPart.Worksheet = new Worksheet(data);
            workbook.Workbook.AppendChild(new Sheets(new Sheet { Id = workbook.GetIdOfPart(sheetPart), SheetId = 1, Name = "Exportação" }));

            data.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Row(headers.Select(Text)));
            var index = 0;
            foreach (var row in _rows)
            {
                index++;
                var values = new Dictionary<string, object>
                {
                    ["Matrícula"] = row.EmployeeNumber,
                    ["Nome"] = row.Name,
                    ["Nome da mãe"] = $"MAE FICTICIA {index}",
                    ["CPF"] = row.Cpf,
                    ["Data admissão"] = row.Admission,
                    ["Data demissão"] = row.Termination,
                    ["Status ocupação"] = row.Status,
                    ["Cargo"] = row.JobCode,
                    ["Descr.Cargo"] = row.JobName,
                    ["Uni.Org"] = row.DeptCode,
                    ["Descr.Unid.Org."] = row.DeptName,
                    ["Chave Organização"] = row.ManagementUnit,
                    ["Grupo"] = row.Group,
                    ["Turma"] = row.Team,
                    ["Endereço"] = $"RUA FICTICIO {index}",
                    ["CEP"] = "00000-0" + (index % 10),
                    ["Telefone"] = "(00) 00000-0000",
                    ["PIS"] = "000.00000.00-0",
                    ["Conta bancária"] = "00000-0 FICTICIO",
                };
                var estado = 0;
                var cells = headers.Select(h =>
                {
                    if (h == "Estado") return Text(estado++ == 0 ? "PE FICTICIO" : "Casado FICTICIA");
                    if (h.StartsWith("Campo SAP", StringComparison.Ordinal)) return Text($"FICTICIO {h} {index}");
                    var value = values.GetValueOrDefault(h, string.Empty);
                    return value is long or int ? Number(Convert.ToInt64(value)) : Text(value.ToString()!);
                });
                data.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Row(cells));
            }
            workbook.Workbook.Save();
        }
        return buffer.ToArray();
    }

    private static Cell Text(string value) => new() { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };

    private static Cell Number(long value) => new() { DataType = CellValues.Number, CellValue = new CellValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture)) };

    /// <summary>CPF válido (dígitos verificadores corretos) a partir de 9 dígitos de base.</summary>
    public static string Cpf(string baseNineDigits)
    {
        var digits = baseNineDigits.Select(c => c - '0').ToList();
        for (var round = 0; round < 2; round++)
        {
            var weight = digits.Count + 1;
            var sum = digits.Sum(d => d * weight--);
            var remainder = sum % 11;
            digits.Add(remainder < 2 ? 0 : 11 - remainder);
        }
        return string.Concat(digits);
    }

    /// <summary>CPF sintético e determinístico para a posição <paramref name="seed"/> (nunca repete entre seeds diferentes).</summary>
    public static string Cpf(int seed) => Cpf((300000000 + seed).ToString("D9"));
}
