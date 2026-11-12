namespace SIGH.Application.Options;

public class JwtOptions
{
    public const string SectionName = "JwtOptions";

    /// <summary>
    /// Tamanho mínimo da chave HMAC-SHA256, em bytes UTF-8 (256 bits).
    /// </summary>
    public const int MinimumSecretKeyBytes = 32;

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; } = 60;
}
