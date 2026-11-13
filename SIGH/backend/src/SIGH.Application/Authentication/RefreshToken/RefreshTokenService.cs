using Microsoft.Extensions.Options;
using SIGH.Application.Authentication.Common;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Application.Security;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using RefreshTokenEntity = SIGH.Domain.Entities.RefreshToken;

namespace SIGH.Application.Authentication.RefreshToken;

public static class RefreshTokenRevocationReasons
{
    public const string Rotated = "Rotacionado";
    public const string Expired = "Token expirado";
    public const string ReuseDetected = "Revogação automática por detecção de reuso de token (Token Reuse Detection)";
}

public class RefreshTokenService : IRefreshTokenService
{
    /// <summary>Quantos sucessores seguir na cadeia de rotação dentro da janela de tolerância.</summary>
    private const int MaxSuccessorHops = 5;

    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityAuditLogger _securityAudit;
    private readonly JwtOptions _jwtOptions;
    private readonly TokenOptions _tokenOptions;
    private readonly AuthOptions _authOptions;

    public RefreshTokenService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        ISecurityAuditLogger securityAudit,
        IOptions<JwtOptions> jwtOptions,
        IOptions<TokenOptions> tokenOptions,
        IOptions<AuthOptions> authOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _securityAudit = securityAudit;
        _jwtOptions = jwtOptions.Value;
        _tokenOptions = tokenOptions.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BusinessRuleValidationException("RefreshToken é obrigatório.");
        }

        var now = _dateTimeProvider.UtcNow;
        var incomingHash = _tokenHasher.HashToken(request.RefreshToken);

        var tokenEntity = await _userRepository.GetRefreshTokenByHashAsync(incomingHash, cancellationToken);

        if (tokenEntity == null)
        {
            throw new BusinessRuleValidationException("Token de atualização inválido ou inexistente.");
        }

        if (tokenEntity.RevokedAt.HasValue)
        {
            // Janela de tolerância: duas abas renovando ao mesmo tempo apresentam o mesmo cookie.
            // A segunda chega com o token recém-rotacionado; isso não é reuso malicioso.
            if (IsRecentlyRotated(tokenEntity, now))
            {
                var successor = await FindActiveSuccessorAsync(tokenEntity, now, cancellationToken);
                if (successor != null)
                {
                    return await IssueTokensAsync(successor.UserId, request, now, rotate: null, cancellationToken);
                }
            }

            await RevokeEverythingForReuseAsync(tokenEntity.UserId, now, cancellationToken);

            throw new BusinessRuleValidationException("Alerta de Segurança: Tentativa de reuso de token detectada. Todas as sessões e tokens foram revogados.");
        }

        if (tokenEntity.ExpiresAt <= now)
        {
            tokenEntity.RevokedAt = now;
            tokenEntity.ReasonRevoked = RefreshTokenRevocationReasons.Expired;
            await _context.SaveChangesAsync(cancellationToken);

            throw new BusinessRuleValidationException("Token de atualização expirado. Por favor, faça login novamente.");
        }

        return await IssueTokensAsync(tokenEntity.UserId, request, now, rotate: tokenEntity, cancellationToken);
    }

    private bool IsRecentlyRotated(RefreshTokenEntity token, DateTimeOffset now) =>
        token.ReasonRevoked == RefreshTokenRevocationReasons.Rotated
        && !string.IsNullOrEmpty(token.ReplacedByTokenHash)
        && token.RevokedAt.HasValue
        && now - token.RevokedAt.Value <= TimeSpan.FromSeconds(Math.Max(0, _authOptions.RefreshTokenReuseGraceSeconds));

    /// <summary>
    /// Segue ReplacedByTokenHash até achar um sucessor ativo. Só atravessa tokens que também foram
    /// rotacionados dentro da janela (várias abas podem ter renovado em sequência).
    /// </summary>
    private async Task<RefreshTokenEntity?> FindActiveSuccessorAsync(RefreshTokenEntity token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = token;
        for (var hop = 0; hop < MaxSuccessorHops && !string.IsNullOrEmpty(current.ReplacedByTokenHash); hop++)
        {
            var next = await _userRepository.GetRefreshTokenByHashAsync(current.ReplacedByTokenHash!, cancellationToken);
            if (next == null || next.UserId != token.UserId)
                return null;

            if (!next.RevokedAt.HasValue && next.ExpiresAt > now)
                return next;

            if (!IsRecentlyRotated(next, now))
                return null;

            current = next;
        }

        return null;
    }

    private async Task RevokeEverythingForReuseAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var activeTokens = await _userRepository.GetActiveRefreshTokensByUserIdAsync(userId, cancellationToken);
        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAt = now;
            activeToken.ReasonRevoked = RefreshTokenRevocationReasons.ReuseDetected;
        }

        var activeSessions = await _userRepository.GetActiveUserSessionsByUserIdAsync(userId, cancellationToken);
        foreach (var activeSession in activeSessions)
        {
            activeSession.IsRevoked = true;
            activeSession.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _securityAudit.RecordAsync(
            SecurityAuditActions.RefreshTokenReuseDetected,
            userId,
            new { revokedTokens = activeTokens.Count, revokedSessions = activeSessions.Count },
            cancellationToken);
    }

    /// <summary>
    /// Emite access token + refresh token novos. Com <paramref name="rotate"/>, o token apresentado
    /// é revogado como "Rotacionado". Sem ele (janela de tolerância), nada é revogado: o sucessor
    /// que a outra aba recebeu continua válido e esta aba ganha o seu próprio refresh token.
    /// </summary>
    private async Task<RefreshTokenResponse> IssueTokensAsync(
        Guid userId,
        RefreshTokenRequest request,
        DateTimeOffset now,
        RefreshTokenEntity? rotate,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId, cancellationToken);

        if (user == null || user.Status == UserStatus.Inactive || user.Status == UserStatus.Suspended || user.Status == UserStatus.Locked)
        {
            throw new BusinessRuleValidationException("Usuário inativo, bloqueado ou não encontrado.");
        }

        var newRawRefreshToken = _refreshTokenGenerator.GenerateRefreshToken();
        var newTokenHash = _tokenHasher.HashToken(newRawRefreshToken);

        if (rotate != null)
        {
            rotate.RevokedAt = now;
            rotate.ReasonRevoked = RefreshTokenRevocationReasons.Rotated;
            rotate.ReplacedByTokenHash = newTokenHash;
        }

        var newTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationInDays),
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        await _userRepository.AddRefreshTokenAsync(newTokenEntity, cancellationToken);

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user, user.GetActiveRoleNames(now), user.GetActivePermissionCodes(now), user.GetNextAccessChange(now));

        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            newAccessToken,
            newRawRefreshToken,
            _jwtOptions.ExpiryInMinutes > 0 ? _jwtOptions.ExpiryInMinutes : 60,
            newTokenEntity.ExpiresAt
        );
    }
}
