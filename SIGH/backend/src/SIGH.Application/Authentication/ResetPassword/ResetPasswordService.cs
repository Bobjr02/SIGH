using Microsoft.Extensions.Options;
using SIGH.Application.Authentication.PasswordTokens;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Validators;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Application.Security;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.ResetPassword;

/// <summary>
/// Redefinição (ou definição no primeiro acesso) com o token do e-mail: uso único, guardado como
/// hash e com validade. Depois do reset, todos os refresh tokens e sessões do usuário são revogados.
/// </summary>
public class ResetPasswordService : IResetPasswordService
{
    public const string InvalidTokenMessage = "Link de redefinição inválido ou expirado. Solicite um novo.";

    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordTokenService _tokens;
    private readonly IDateTimeProvider _clock;
    private readonly ISecurityAuditLogger _securityAudit;
    private readonly PasswordOptions _passwordOptions;

    public ResetPasswordService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordTokenService tokens,
        IDateTimeProvider clock,
        ISecurityAuditLogger securityAudit,
        IOptions<PasswordOptions> passwordOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _clock = clock;
        _securityAudit = securityAudit;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _tokens.FindUsableAsync(request.Token, cancellationToken)
            ?? throw new BusinessRuleValidationException(InvalidTokenMessage);

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null || user.Status is UserStatus.Inactive or UserStatus.Suspended)
            throw new BusinessRuleValidationException(InvalidTokenMessage);

        var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.NewPassword, _passwordOptions);
        if (!isValid)
            throw new BusinessRuleValidationException(errorMessage);

        var recentHistories = await _userRepository.GetRecentPasswordHistoriesAsync(user.Id, _passwordOptions.PasswordHistoryLimit, cancellationToken);
        var recentPasswordHashes = recentHistories.Select(ph => ph.PasswordHash).Append(user.PasswordHash);
        if (recentPasswordHashes.Any(hash => _passwordHasher.VerifyPassword(request.NewPassword, hash)))
            throw new BusinessRuleValidationException($"A nova senha não pode ser igual a nenhuma das últimas {_passwordOptions.PasswordHistoryLimit} senhas utilizadas.");

        var now = _clock.UtcNow;
        var newHash = _passwordHasher.HashPassword(request.NewPassword);

        user.PasswordHash = newHash;
        user.RotateSecurityStamp();
        user.MustChangePassword = false;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        if (user.Status is UserStatus.PendingFirstAccess or UserStatus.Locked)
            user.Status = UserStatus.Active;

        await _userRepository.AddPasswordHistoryAsync(new PasswordHistory { UserId = user.Id, PasswordHash = newHash }, cancellationToken);

        // Uso único: este e qualquer outro link pendente do usuário deixam de valer.
        token.MarkUsed(now);
        await _tokens.InvalidateAllAsync(user.Id, cancellationToken);

        // Quem tinha a senha antiga perde as sessões abertas.
        var activeTokens = await _userRepository.GetActiveRefreshTokensByUserIdAsync(user.Id, cancellationToken);
        foreach (var refreshToken in activeTokens)
        {
            refreshToken.RevokedAt = now;
            refreshToken.ReasonRevoked = "Senha redefinida";
        }

        var activeSessions = await _userRepository.GetActiveUserSessionsByUserIdAsync(user.Id, cancellationToken);
        foreach (var session in activeSessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _securityAudit.RecordAsync(SecurityAuditActions.PasswordReset, user.Id,
            new { purpose = token.Purpose, revokedRefreshTokens = activeTokens.Count, revokedSessions = activeSessions.Count }, cancellationToken);

        return new ResetPasswordResponse(true, "Senha redefinida com sucesso. Entre com a nova senha.");
    }
}
