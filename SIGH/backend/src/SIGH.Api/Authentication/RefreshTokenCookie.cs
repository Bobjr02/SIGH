namespace SIGH.Api.Authentication;

/// <summary>
/// O refresh token só trafega neste cookie: HttpOnly (inacessível ao JavaScript), Secure,
/// SameSite=Strict e restrito a /api/v1/auth. Nunca aparece no corpo das respostas.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "sigh_rt";
    public const string Path = "/api/v1/auth";

    /// <param name="persistent">
    /// "Lembrar-me". Falso: cookie de SESSÃO (sem Expires; o navegador o apaga ao fechar). O token em si
    /// continua expirando em <paramref name="expiresAt"/> no servidor.
    /// </param>
    public static void Append(HttpResponse response, string refreshToken, DateTimeOffset expiresAt, bool persistent = true)
    {
        response.Cookies.Append(Name, refreshToken, CreateOptions(persistent ? expiresAt : null));
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, CreateOptions(expiresAt: null));
    }

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static CookieOptions CreateOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true
    };
}
