namespace SIGH.Application.Options;

/// <summary>Configuração de autenticação (seção "Auth").</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Janela, em segundos, em que um refresh token já rotacionado ainda é aceito sem disparar a
    /// detecção de reuso. Cobre duas abas renovando ao mesmo tempo com o mesmo cookie.
    /// </summary>
    public int RefreshTokenReuseGraceSeconds { get; set; } = 30;

    /// <summary>Validade do link de redefinição de senha ("esqueci a senha").</summary>
    public int PasswordResetTokenMinutes { get; set; } = 30;

    /// <summary>Validade do link de definição de senha enviado quando a conta é criada.</summary>
    public int AccountSetupTokenHours { get; set; } = 72;

    /// <summary>Teto do cache do SecurityStamp: a perda de acesso nunca demora mais que isto.</summary>
    public const int MaxSecurityStampCacheSeconds = 30;

    /// <summary>
    /// Por quanto tempo (segundos) o stamp e o status do usuário ficam em cache na API antes de serem
    /// relidos do banco. Limitado a <see cref="MaxSecurityStampCacheSeconds"/>; 0 desliga o cache.
    /// </summary>
    public int SecurityStampCacheSeconds { get; set; } = MaxSecurityStampCacheSeconds;
}
