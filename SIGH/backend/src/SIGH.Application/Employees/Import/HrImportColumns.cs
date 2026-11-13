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

public sealed record HrColumn(HrField Field, bool Required, params string[] Headers);

/// <summary>
/// LISTA FECHADA de colunas da importação. É o único ponto que define o que é lido do arquivo.
///
/// Os cabeçalhos de Matrícula, Cargo, Descr.Cargo, Uni.Org, Descr.Unid.Org., Chave Organização,
/// Data admissão e Data demissão vêm da especificação do sprint. Os de nome, CPF, situação, grupo e
/// turma são SUPOSIÇÕES até a conferência com SIGH/docs/referencias/LEIAME.md (seção 2), que não
/// estava disponível quando este código foi escrito: ajuste os cabeçalhos aqui se forem diferentes.
/// </summary>
public static class HrImportColumns
{
    public static readonly IReadOnlyList<HrColumn> All =
    [
        new(HrField.EmployeeNumber, true, "Matrícula"),
        new(HrField.FullName, true, "Nome", "Nome completo"),
        new(HrField.Cpf, true, "CPF", "Nº CPF"),
        new(HrField.AdmissionDate, true, "Data admissão"),
        new(HrField.TerminationDate, false, "Data demissão"),
        new(HrField.Status, true, "Status", "Situação", "Status ocupação"),
        new(HrField.JobTitleCode, true, "Cargo"),
        new(HrField.JobTitleName, true, "Descr.Cargo"),
        new(HrField.DepartmentCode, true, "Uni.Org"),
        new(HrField.DepartmentName, true, "Descr.Unid.Org."),
        new(HrField.ManagementUnitCode, true, "Chave Organização"),
        new(HrField.Group, false, "Grupo"),
        new(HrField.Team, false, "Turma"),
    ];

    /// <summary>Nome exibido nas mensagens (o primeiro cabeçalho aceito).</summary>
    public static string DisplayName(HrField field) => All.First(c => c.Field == field).Headers[0];

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
