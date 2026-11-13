using System.Globalization;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Employees.Helpers;

namespace SIGH.Application.Employees.Import;

public enum HrIssueSeverity
{
    Error = 1,
    Warning = 2
}

/// <summary>
/// Problema numa linha. Identifica a linha pelo número no Excel e pela matrícula, NUNCA pelo CPF
/// nem por colunas fora da lista fechada.
/// </summary>
public sealed record HrImportIssue(int? RowNumber, string? EmployeeNumber, HrIssueSeverity Severity, string Message);

/// <summary>Linha válida, já convertida.</summary>
public sealed record HrRow(
    int RowNumber,
    string EmployeeNumber,
    string FullName,
    string Cpf,
    DateOnly AdmissionDate,
    DateOnly? TerminationDate,
    EmployeeStatus Status,
    string JobTitleCode,
    string JobTitleName,
    string DepartmentCode,
    string DepartmentName,
    string ManagementUnitCode,
    EmployeeGroup? Group,
    string? Team);

public static class HrRowValidator
{
    public const int EmployeeNumberLength = 8;
    public const int CpfLength = 11;
    public const int FullNameMaxLength = 150;
    public const string DateFormat = "dd/MM/yyyy";

    // Situações conhecidas da exportação (normalizadas). Qualquer outra rejeita a linha: nunca adivinhamos.
    private static readonly Dictionary<string, EmployeeStatus> KnownStatuses = new()
    {
        ["ativo"] = EmployeeStatus.Active,
        ["ativa"] = EmployeeStatus.Active,
        ["afastado"] = EmployeeStatus.OnLeave,
        ["afastada"] = EmployeeStatus.OnLeave,
        ["desligado"] = EmployeeStatus.Terminated,
        ["desligada"] = EmployeeStatus.Terminated,
        ["demitido"] = EmployeeStatus.Terminated,
        ["demitida"] = EmployeeStatus.Terminated,
    };

    private static readonly Dictionary<string, EmployeeGroup> KnownGroups = new()
    {
        ["campo"] = EmployeeGroup.Field,
        ["industria"] = EmployeeGroup.Industry,
    };

    /// <summary>Valida e converte uma linha. Devolve a linha válida ou null, acrescentando os problemas em <paramref name="issues"/>.</summary>
    public static HrRow? Validate(HrRawRow raw, List<HrImportIssue> issues)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var number = Digits(raw.Get(HrField.EmployeeNumber), EmployeeNumberLength);
        var numberForMessages = number.Length > 0 ? number : null;
        if (number.Length != EmployeeNumberLength || !number.All(char.IsAsciiDigit))
            errors.Add($"Matrícula inválida: deve ter {EmployeeNumberLength} dígitos.");

        var name = raw.Get(HrField.FullName).Text.Trim();
        if (name.Length == 0) errors.Add("Nome obrigatório.");
        else if (name.Length > FullNameMaxLength) errors.Add($"Nome com mais de {FullNameMaxLength} caracteres.");

        var cpf = Digits(raw.Get(HrField.Cpf), CpfLength, allowFormatting: true);
        if (cpf.Length == 0) errors.Add("CPF obrigatório.");
        else if (!CpfValidator.IsValid(cpf)) errors.Add("CPF inválido (dígitos verificadores não conferem).");

        var admission = ParseDate(raw.Get(HrField.AdmissionDate).Text, HrImportColumns.DisplayName(HrField.AdmissionDate), required: true, errors);
        var termination = ParseDate(raw.Get(HrField.TerminationDate).Text, HrImportColumns.DisplayName(HrField.TerminationDate), required: false, errors);

        var statusText = raw.Get(HrField.Status).Text.Trim();
        EmployeeStatus? status = null;
        if (statusText.Length == 0)
            errors.Add("Situação obrigatória.");
        else if (KnownStatuses.TryGetValue(HrImportColumns.Normalize(statusText), out var known))
            status = known;
        else
            errors.Add($"Situação \"{Truncate(statusText, 40)}\" desconhecida. Valores aceitos: Ativo, Afastado, Desligado ou Demitido.");

        if (status == EmployeeStatus.Terminated && termination is null && !errors.Any(e => e.Contains("Data demissão")))
            errors.Add("Situação de desligamento exige a Data demissão preenchida.");
        if (status is EmployeeStatus.Active or EmployeeStatus.OnLeave && termination is not null)
        {
            warnings.Add($"Data demissão ignorada: a situação é \"{Truncate(statusText, 40)}\".");
            termination = null;
        }
        if (admission is not null && termination is not null && termination < admission)
            errors.Add("Data demissão anterior à Data admissão.");

        var jobCode = Required(raw, HrField.JobTitleCode, errors);
        var jobName = raw.Get(HrField.JobTitleName).Text.Trim();
        var deptCode = Required(raw, HrField.DepartmentCode, errors);
        var deptName = raw.Get(HrField.DepartmentName).Text.Trim();
        var muCode = Required(raw, HrField.ManagementUnitCode, errors).ToUpperInvariant();
        if (jobCode.Length > 0 && jobName.Length == 0)
        {
            warnings.Add($"Descr.Cargo vazia: o cargo {jobCode} usa o código como descrição.");
            jobName = jobCode;
        }
        if (deptCode.Length > 0 && deptName.Length == 0)
        {
            warnings.Add($"Descr.Unid.Org. vazia: o setor {deptCode} usa o código como descrição.");
            deptName = deptCode;
        }

        EmployeeGroup? group = null;
        var groupText = raw.Get(HrField.Group).Text.Trim();
        if (groupText.Length > 0)
        {
            if (KnownGroups.TryGetValue(HrImportColumns.Normalize(groupText), out var g)) group = g;
            else errors.Add($"Grupo \"{Truncate(groupText, 30)}\" desconhecido. Valores aceitos: Campo ou Indústria.");
        }

        var team = raw.Get(HrField.Team).Text.Trim();
        if (team.Length > SIGH.Domain.Employees.Entities.Employee.TeamMaxLength)
            errors.Add($"Turma com mais de {SIGH.Domain.Employees.Entities.Employee.TeamMaxLength} caracteres.");

        foreach (var w in warnings) issues.Add(new HrImportIssue(raw.RowNumber, numberForMessages, HrIssueSeverity.Warning, w));
        if (errors.Count > 0)
        {
            foreach (var e in errors) issues.Add(new HrImportIssue(raw.RowNumber, numberForMessages, HrIssueSeverity.Error, e));
            return null;
        }

        return new HrRow(raw.RowNumber, number, name, cpf, admission!.Value, termination, status!.Value,
            jobCode, Truncate(jobName, 150), deptCode, Truncate(deptName, 150), muCode, group, team.Length == 0 ? null : team);
    }

    /// <summary>
    /// Só dígitos. Célula numérica (Excel tira zeros à esquerda) é completada com zeros até <paramref name="length"/>.
    /// CPF em texto pode vir formatado (123.456.789-09); matrícula em texto precisa ser só dígitos.
    /// </summary>
    private static string Digits(HrCell cell, int length, bool allowFormatting = false)
    {
        var text = cell.Text.Trim();
        if (text.Length == 0) return string.Empty;
        if (cell.WasNumber)
        {
            var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
            return digits.Length < length ? digits.PadLeft(length, '0') : digits;
        }
        if (allowFormatting && text.All(c => char.IsAsciiDigit(c) || c is '.' or '-' or ' '))
            return new string(text.Where(char.IsAsciiDigit).ToArray());
        return text;
    }

    private static DateOnly? ParseDate(string text, string column, bool required, List<string> errors)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            if (required) errors.Add($"{column} obrigatória.");
            return null;
        }
        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        errors.Add($"{column} inválida: use o formato {DateFormat}.");
        return null;
    }

    private static string Required(HrRawRow raw, HrField field, List<string> errors)
    {
        var text = raw.Get(field).Text.Trim();
        if (text.Length == 0) errors.Add($"{HrImportColumns.DisplayName(field)} obrigatório.");
        return text;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
}
