using SIGH.Domain.Common;

namespace SIGH.Domain.Entities;

public enum PasswordTokenPurpose
{
    /// <summary>"Esqueci a senha".</summary>
    PasswordReset = 1,
    /// <summary>Definição da senha no primeiro acesso, enviada quando a conta é criada.</summary>
    AccountSetup = 2
}

/// <summary>
/// Token de redefinição/definição de senha. Só o hash (SHA-256) é guardado; o valor bruto vai
/// apenas no link do e-mail. Uso único e com validade.
/// </summary>
public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public PasswordTokenPurpose Purpose { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>Quando foi usado (ou invalidado por um token mais novo / pela redefinição).</summary>
    public DateTimeOffset? UsedAt { get; private set; }
    public string? RequestedFromIp { get; private set; }

    protected PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash, PasswordTokenPurpose purpose, DateTimeOffset now, TimeSpan lifetime, string? requestedFromIp)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuário obrigatório.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("Hash obrigatório.", nameof(tokenHash));
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
            RequestedFromIp = requestedFromIp
        };
    }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now) => UsedAt ??= now;
}
