using SIGH.Application.Security;
using Microsoft.Extensions.Options;
using SIGH.Application.Authentication.Common;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Validators;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using RefreshTokenEntity = SIGH.Domain.Entities.RefreshToken;

namespace SIGH.Application.Authentication.ChangePassword;

public class ChangePasswordService : IChangePasswordService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PasswordOptions _passwordOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly TokenOptions _tokenOptions;
    private readonly ISecurityAuditLogger _securityAudit;

    public ChangePasswordService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<PasswordOptions> passwordOptions,
        IOptions<JwtOptions> jwtOptions,
        IOptions<TokenOptions> tokenOptions,
        ISecurityAuditLogger securityAudit)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _passwordOptions = passwordOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _tokenOptions = tokenOptions.Value;
        _securityAudit = securityAudit;
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Validar complexidade da nova senha
        var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.NewPassword, _passwordOptions);
        if (!isValid)
        {
            throw new BusinessRuleValidationException(errorMessage);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("Usuário não encontrado.");
        }

        // Validar senha atual
        bool isCurrentPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            throw new BusinessRuleValidationException("A senha atual informada está incorreta.");
        }

        // Verificar histórico das últimas N senhas (padrão 3)
        var recentHistories = await _userRepository.GetRecentPasswordHistoriesAsync(user.Id, _passwordOptions.PasswordHistoryLimit, cancellationToken);
        var recentPasswordHashes = recentHistories.Select(ph => ph.PasswordHash).ToList();

        recentPasswordHashes.Add(user.PasswordHash);

        foreach (var oldHash in recentPasswordHashes)
        {
            if (_passwordHasher.VerifyPassword(request.NewPassword, oldHash))
            {
                throw new BusinessRuleValidationException($"A nova senha não pode ser igual a nenhuma das últimas {_passwordOptions.PasswordHistoryLimit} senhas utilizadas.");
            }
        }

        var newHash = _passwordHasher.HashPassword(request.NewPassword);

        user.PasswordHash = newHash;
        user.MustChangePassword = false;

        if (user.Status == UserStatus.PendingFirstAccess)
        {
            user.Status = UserStatus.Active;
        }

        await _userRepository.AddPasswordHistoryAsync(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = newHash
        }, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        // Senha trocada: tokens emitidos antes deixam de valer (o par novo abaixo já leva o stamp novo).
        user.RotateSecurityStamp();

        // Revoga todos os refresh tokens anteriores: sessões abertas com a senha antiga
        // (inclusive as emitidas com a claim pwd_change_required) não podem mais ser renovadas.
        var activeRefreshTokens = await _userRepository.GetActiveRefreshTokensByUserIdAsync(user.Id, cancellationToken);
        foreach (var activeToken in activeRefreshTokens)
        {
            activeToken.RevokedAt = now;
            activeToken.ReasonRevoked = "Senha alterada pelo usuário";
        }

        // Novo par de tokens, já refletindo MustChangePassword = false.
        var userWithAccess = await _userRepository.GetByIdWithRolesAndPermissionsAsync(user.Id, cancellationToken);
        var roles = userWithAccess?.GetActiveRoleNames(now) ?? new List<string>();
        var permissions = userWithAccess?.GetActivePermissionCodes(now) ?? new List<string>();

        var accessToken = _jwtTokenGenerator.GenerateToken(user, roles, permissions, userWithAccess?.GetNextAccessChange(now));
        var rawRefreshToken = _refreshTokenGenerator.GenerateRefreshToken();

        var refreshTokenExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationInDays);

        await _userRepository.AddRefreshTokenAsync(new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = _tokenHasher.HashToken(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _securityAudit.RecordAsync(SecurityAuditActions.PasswordChanged, user.Id,
            new { revokedRefreshTokens = activeRefreshTokens.Count }, cancellationToken);

        return new ChangePasswordResponse(
            true,
            "Senha alterada com sucesso.",
            accessToken,
            rawRefreshToken,
            _jwtOptions.ExpiryInMinutes > 0 ? _jwtOptions.ExpiryInMinutes : 60,
            refreshTokenExpiresAt);
    }
}
