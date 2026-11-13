using System.Globalization;
using System.Text;

namespace SIGH.Application.Employees.Import;

/// <summary>Campos lidos da exportação do SAP. Qualquer outra coluna é ignorada (nunca lida, gravada ou logada).</summary>
public enum HrField
{
    EmployeeNumber,
    FullName,
    Cpf,
    AdmissionDate,
    TerminationDate,
    Status,
    JobTitleCode,
    JobTitleName,
    DepartmentCode,
    DepartmentName,
    ManagementUnitCode,
    Group,
    Team
}

/// <param name="Label">Nome do campo no SIGH, usado nos avisos ("o campo Grupo ficará vazio").</param>
public sealed record HrColumn(HrField Field, bool Required, string Header, string Label);

/// <summary>
/// LISTA FECHADA de colunas da importação, conforme SIGH/docs/referencias/LEIAME.md, seção 2,
/// tabela "Colunas que o SIGH importa", e conferida contra os arquivos fictícios do pacote
/// (72 colunas, aba "Data"). É o único ponto que define o que é lido do arquivo: qualquer outra coluna
/// (inclusive "Turma", que é só o código, e as duas "Estado") é ignorada.
/// </summary>
public static class HrImportColumns
{
    public static readonly IReadOnlyList<HrColumn> All =
    [
        new(HrField.EmployeeNumber, true, "Matrícula", "Matrícula"),
        new(HrField.FullName, true, "Nome", "Nome"),
        new(HrField.Cpf, true, "CPF", "CPF"),
        new(HrField.Status, true, "Status", "Situação"),
        new(HrField.Group, false, "Grupo_Emp", "Grupo"),
        new(HrField.JobTitleCode, true, "Cargo", "Cargo"),
        new(HrField.JobTitleName, true, "Descr.Cargo", "Descrição do cargo"),
        new(HrField.DepartmentCode, true, "Uni.Org", "Setor"),
        new(HrField.DepartmentName, true, "Descr.Unid.Org.", "Descrição do setor"),
        new(HrField.AdmissionDate, true, "Data admissão", "Admissão"),
        new(HrField.TerminationDate, false, "Data demissão", "Demissão"),
        new(HrField.Team, false, "Descrição Turma", "Turma"),
        new(HrField.ManagementUnitCode, true, "Chave Organização", "Gerência"),
    ];

    /// <summary>Cabeçalho exibido nas mensagens.</summary>
    public static string DisplayName(HrField field) => All.First(c => c.Field == field).Header;

    /// <summary>Sem acento, minúsculo e só letras e dígitos: "Descr. Unid. Org." == "descr.unid.org" == "DESCRUNIDORG".</summary>
    public static string Normalize(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return string.Empty;
        var decomposed = header.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }
}
