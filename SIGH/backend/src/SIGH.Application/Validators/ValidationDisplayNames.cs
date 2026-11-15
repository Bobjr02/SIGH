using System.Text.RegularExpressions;

namespace SIGH.Application.Validators;

/// <summary>
/// Nome do campo nas mensagens padrão do FluentValidation ("'{PropertyName}' não pode ser vazio."):
/// em português, nunca o nome técnico da propriedade.
/// </summary>
public static partial class ValidationDisplayNames
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Email"] = "E-mail",
        ["Password"] = "Senha",
        ["CurrentPassword"] = "Senha atual",
        ["NewPassword"] = "Nova senha",
        ["FullName"] = "Nome completo",
        ["Cpf"] = "CPF",
        ["Description"] = "Descrição",
        ["LegalReference"] = "Fundamento legal",
        ["Location"] = "Local",
        ["Name"] = "Nome",
        ["Code"] = "Código",
        ["Title"] = "Título",
        ["Reason"] = "Motivo",
        ["Token"] = "Token",
        ["PageNumber"] = "Página",
        ["PageSize"] = "Itens por página",
        ["EmployeeNumber"] = "Matrícula",
        ["Status"] = "Situação",
    };

    public static string Of(string propertyName) =>
        Names.TryGetValue(propertyName, out var name) ? name : Humanize(propertyName);

    /// <summary>Fallback legível: "AdmissionDate" → "Admission date" (nunca o código colado).</summary>
    private static string Humanize(string propertyName)
    {
        var words = Words().Replace(propertyName, " $1").Trim().ToLowerInvariant();
        return words.Length == 0 ? propertyName : char.ToUpperInvariant(words[0]) + words[1..];
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex Words();
}
