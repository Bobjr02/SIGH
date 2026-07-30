namespace SIGH.Application.Disciplinary.Reports.Utils;

public static class CsvSanitizer
{
    private static readonly char[] FormulaPrefixes = { '=', '+', '-', '@' };

    public static string SanitizeField(string? input, string separator = ";")
    {
        if (string.IsNullOrEmpty(input))
        {
            return "\"\"";
        }

        string trimmed = input.Trim();

        if (trimmed.Length > 0 && Array.IndexOf(FormulaPrefixes, trimmed[0]) >= 0)
        {
            trimmed = "'" + trimmed;
        }

        if (trimmed.Contains("\""))
        {
            trimmed = trimmed.Replace("\"", "\"\"");
        }

        return $"\"{trimmed}\"";
    }
}
