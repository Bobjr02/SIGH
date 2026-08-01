using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using RefreshTokenEntity = SIGH.Domain.Entities.RefreshToken;

namespace SIGH.Application.Authentication.RefreshToken;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;
    private readonly TokenOptions _tokenOptions;

    public RefreshTokenService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions,
        IOptions<TokenOptions> tokenOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
        _tokenOptions = tokenOptions.Value;
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BusinessRuleValidationException("RefreshToken é obrigatório.");
        }

        var incomingHash = _tokenHasher.HashToken(request.RefreshToken);

        var tokenEntity = await _userRepository.GetRefreshTokenByHashAsync(incomingHash, cancellationToken);

        // DETECÇÃO DE REUSO DE TOKEN (TOKEN REUSE DETECTION)
        if (tokenEntity == null || tokenEntity.RevokedAt.HasValue)
        {
            if (tokenEntity != null)
            {
                // Reuso detectado de um token que já foi revogado/rotacionado!
                // Revogar toda a árvore de tokens e sessões ativas do usuário por segurança.
                var userId = tokenEntity.UserId;
                var now = _dateTimeProvider.UtcNow;

                var activeTokens = await _userRepository.GetActiveRefreshTokensByUserIdAsync(userId, cancellationToken);

                foreach (var activeToken in activeTokens)
                {
                    activeToken.RevokedAt = now;
                    activeToken.ReasonRevoked = "Revogação automática por detecção de reuso de token (Token Reuse Detection)";
                }

                var activeSessions = await _userRepository.GetActiveUserSessionsByUserIdAsync(userId, cancellationToken);

                foreach (var activeSession in activeSessions)
                {
                    activeSession.IsRevoked = true;
                    activeSession.RevokedAt = now;
                }

                await _context.SaveChangesAsync(cancellationToken);

                throw new BusinessRuleValidationException("Alerta de Segurança: Tentativa de reuso de token detectada. Todas as sessões e tokens foram revogados.");
            }

            throw new BusinessRuleValidationException("Token de atualização inválido ou inexistente.");
        }

        // Verificar Expiração
        if (tokenEntity.ExpiresAt <= _dateTimeProvider.UtcNow)
        {
            tokenEntity.RevokedAt = _dateTimeProvider.UtcNow;
            tokenEntity.ReasonRevoked = "Token expirado";
            await _context.SaveChangesAsync(cancellationToken);

            throw new BusinessRuleValidationException("Token de atualização expirado. Por favor, faça login novamente.");
        }

        // Carregar Usuário e Permissões
        var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(tokenEntity.UserId, cancellationToken);

        if (user == null || user.Status == UserStatus.Inactive || user.Status == UserStatus.Suspended || user.Status == UserStatus.Locked)
        {
            throw new BusinessRuleValidationException("Usuário inativo, bloqueado ou não encontrado.");
        }

        var rotationTime = _dateTimeProvider.UtcNow;

        // Rotação de Token: Revogar antigo e criar novo
        var newRawRefreshToken = _refreshTokenGenerator.GenerateRefreshToken();
        var newTokenHash = _tokenHasher.HashToken(newRawRefreshToken);

        tokenEntity.RevokedAt = rotationTime;
        tokenEntity.ReasonRevoked = "Rotacionado";
        tokenEntity.ReplacedByTokenHash = newTokenHash;

        var newTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = rotationTime.AddDays(_tokenOptions.RefreshTokenExpirationInDays),
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        await _userRepository.AddRefreshTokenAsync(newTokenEntity, cancellationToken);

        // Extrair Roles e Permissões
        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => !rp.IsDeleted && rp.Permission != null && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user, roles, permissions);

        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            newAccessToken,
            newRawRefreshToken,
            _jwtOptions.ExpiryInMinutes > 0 ? _jwtOptions.ExpiryInMinutes : 60
        );
    }
}
