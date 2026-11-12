using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;

namespace SIGH.Application.Authentication.PasswordTokens;

/// <summary>Tokens de redefinição e de definição de senha (primeiro acesso).</summary>
public interface IPasswordTokenService
{
    /// <summary>
    /// Gera um token novo (só o hash é guardado) e invalida os anteriores ainda não usados do usuário.
    /// Não grava: entra no SaveChanges do caso de uso. Devolve o valor bruto, que vai só no link do e-mail.
    /// </summary>
    Task<string> IssueAsync(Guid userId, PasswordTokenPurpose purpose, CancellationToken cancellationToken = default, string? requestedFromIp = null);

    /// <summary>Token válido (existe, não usado, não expirado) ou null.</summary>
    Task<PasswordResetToken?> FindUsableAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>Marca como usados todos os tokens pendentes do usuário.</summary>
    Task InvalidateAllAsync(Guid userId, CancellationToken cancellationToken = default);

    TimeSpan LifetimeOf(PasswordTokenPurpose purpose);
}

public sealed class PasswordTokenService : IPasswordTokenService
{
    // 48 caracteres alfanuméricos ≈ 285 bits de entropia.
    private const int TokenLength = 48;

    private readonly IApplicationDbContext _context;
    private readonly ITokenHasher _hasher;
    private readonly IRandomStringGenerator _random;
    private readonly IDateTimeProvider _clock;
    private readonly IRequestContext _requestContext;
    private readonly AuthOptions _options;

    public PasswordTokenService(
        IApplicationDbContext context,
        ITokenHasher hasher,
        IRandomStringGenerator random,
        IDateTimeProvider clock,
        IRequestContext requestContext,
        IOptions<AuthOptions> options)
    {
        _context = context;
        _hasher = hasher;
        _random = random;
        _clock = clock;
        _requestContext = requestContext;
        _options = options.Value;
    }

    public TimeSpan LifetimeOf(PasswordTokenPurpose purpose) => purpose == PasswordTokenPurpose.AccountSetup
        ? TimeSpan.FromHours(Math.Max(1, _options.AccountSetupTokenHours))
        : TimeSpan.FromMinutes(Math.Max(1, _options.PasswordResetTokenMinutes));

    public async Task<string> IssueAsync(Guid userId, PasswordTokenPurpose purpose, CancellationToken cancellationToken = default, string? requestedFromIp = null)
    {
        await InvalidateAllAsync(userId, cancellationToken);

        var raw = _random.GenerateRandomString(TokenLength);
        _context.PasswordResetTokens.Add(PasswordResetToken.Create(
            userId, _hasher.HashToken(raw), purpose, _clock.UtcNow, LifetimeOf(purpose), requestedFromIp ?? _requestContext.IpAddress));

        return raw;
    }

    public async Task<PasswordResetToken?> FindUsableAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 256)
            return null;

        var hash = _hasher.HashToken(rawToken.Trim());
        var token = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        return token is not null && token.IsUsable(_clock.UtcNow) ? token : null;
    }

    public async Task InvalidateAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var pending = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in pending)
            token.MarkUsed(now);
    }
}
