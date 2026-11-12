namespace SIGH.Application.Options;

/// <summary>Dados públicos da aplicação (seção "App").</summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>URL pública do frontend, usada nos links dos e-mails (ex.: https://sigh.empresa.com.br).</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string BuildUrl(string pathAndQuery) => PublicBaseUrl.TrimEnd('/') + "/" + pathAndQuery.TrimStart('/');
}
