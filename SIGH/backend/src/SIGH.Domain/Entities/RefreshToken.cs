namespace SIGH.Domain.Entities;

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? ReasonRevoked { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>
    /// "Lembrar-me" no login. Falso: o cookie do refresh token é de SESSÃO (some ao fechar o navegador).
    /// Verdadeiro: o cookie expira junto com o token. A escolha passa de token para token na rotação.
    /// </summary>
    public bool IsPersistent { get; set; } = true;

    public RefreshToken() : base() { }

    public RefreshToken(Guid id) : base(id) { }
}
