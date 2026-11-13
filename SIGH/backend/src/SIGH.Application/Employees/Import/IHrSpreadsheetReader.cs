namespace SIGH.Application.Employees.Import;

/// <summary>Célula como texto. WasNumber: a célula era numérica no Excel (para completar zeros à esquerda).</summary>
public readonly record struct HrCell(string Text, bool WasNumber);

/// <summary>Uma linha de dados, só com os campos da lista fechada. RowNumber é a linha no Excel (1 = primeira).</summary>
public sealed record HrRawRow(int RowNumber, IReadOnlyDictionary<HrField, HrCell> Cells)
{
    public HrCell Get(HrField field) => Cells.TryGetValue(field, out var cell) ? cell : default;
}

public sealed record HrSheet(IReadOnlyList<HrRawRow> Rows, IReadOnlyList<string> Warnings);

/// <summary>Arquivo inválido como um todo (não é xlsx, falta coluna obrigatória...). Mensagem segura para o usuário.</summary>
public sealed class HrSpreadsheetException : Exception
{
    public HrSpreadsheetException(string message) : base(message) { }
}

public interface IHrSpreadsheetReader
{
    /// <summary>
    /// Lê a primeira planilha. Encontra o cabeçalho, mapeia SÓ as colunas de <see cref="HrImportColumns"/>
    /// e devolve o texto dessas células. Lança <see cref="HrSpreadsheetException"/> se faltar coluna obrigatória.
    /// </summary>
    HrSheet Read(Stream xlsx);
}
