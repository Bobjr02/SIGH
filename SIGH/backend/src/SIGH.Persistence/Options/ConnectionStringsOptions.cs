namespace SIGH.Persistence.Options;

/// <summary>
/// Seção "ConnectionStrings" da configuração. Validada no startup: a API não sobe sem
/// ConnectionStrings:DefaultConnection.
/// </summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = string.Empty;
}
