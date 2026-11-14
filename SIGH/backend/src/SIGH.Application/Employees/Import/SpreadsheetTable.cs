namespace SIGH.Application.Employees.Import;

/// <summary>Coluna de uma lista fechada de cabeçalhos. Key identifica o campo; Header é o nome exato da planilha.</summary>
/// <param name="ReadValues">false: o cabeçalho tem de existir, mas o conteúdo nunca é lido (ex.: nome do funcionário).</param>
public sealed record SheetColumn(string Key, string Header, bool Required, string Label, bool ReadValues = true);

public sealed record SheetRow(int RowNumber, IReadOnlyDictionary<string, HrCell> Cells)
{
    public HrCell Get(string key) => Cells.TryGetValue(key, out var cell) && cell.Text is not null ? cell : HrCell.Empty;
}

public sealed record SheetTable(IReadOnlyList<SheetRow> Rows, IReadOnlyList<string> Warnings);

/// <summary>
/// Leitor genérico de xlsx por lista fechada de cabeçalhos (normalizados: sem acento, sem diferença de
/// maiúsculas, só letras e dígitos). Usado pela importação de funcionários e pela do histórico disciplinar.
/// Linhas totalmente vazias (nas colunas lidas) são ignoradas. Datas do Excel viram texto dd/MM/yyyy.
/// </summary>
public interface ISpreadsheetTableReader
{
    /// <param name="anchorKey">Coluna que identifica a linha de cabeçalho (ex.: a matrícula).</param>
    SheetTable Read(Stream xlsx, IReadOnlyList<SheetColumn> columns, string anchorKey);
}
