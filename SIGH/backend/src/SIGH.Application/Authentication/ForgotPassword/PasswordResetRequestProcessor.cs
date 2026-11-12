using Microsoft.Extensions.Options;
using SIGH.Application.Authentication.PasswordTokens;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Email;
using SIGH.Application.Options;
using SIGH.Application.Security;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.ForgotPassword;

/// <summary>
/// Processa um pedido da fila (fora do request): se houver conta elegível, gera o token de uso único,
/// enfileira o e-mail na outbox e registra o evento de segurança. E-mail inexistente: não faz nada.
/// </summary>
public sealed class PasswordResetRequestProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordTokenService _tokens;
    private readonly IEmailOutbox _outbox;
    private readonly ISecurityAuditLogger _securityAudit;
    private readonly AppOptions _appOptions;

    public PasswordResetRequestProcessor(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordTokenService tokens,
        IEmailOutbox outbox,
        ISecurityAuditLogger securityAudit,
        IOptions<AppOptions> appOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _tokens = tokens;
        _outbox = outbox;
        _securityAudit = securityAudit;
        _appOptions = appOptions.Value;
    }

    /// <summary>Devolve true se um e-mail foi enfileirado.</summary>
    public async Task<bool> ProcessAsync(PasswordResetRequestJob job, CancellationToken cancellationToken = default)
    {
        var user = string.IsNullOrEmpty(job.Email) ? null : await _userRepository.GetByEmailAsync(job.Email, cancellationToken);

        // Contas inativas ou suspensas não recebem link (o administrador precisa reativá-las).
        if (user is null || user.Status is UserStatus.Inactive or UserStatus.Suspended)
            return false;

        var rawToken = await _tokens.IssueAsync(user.Id, PasswordTokenPurpose.PasswordReset, cancellationToken, job.IpAddress);
        var link = _appOptions.BuildUrl($"/reset-password?token={Uri.EscapeDataString(rawToken)}");
        var minutes = (int)_tokens.LifetimeOf(PasswordTokenPurpose.PasswordReset).TotalMinutes;

        _outbox.Enqueue(user.Email, EmailTemplates.PasswordReset(user.FullName, link, minutes));
        await _context.SaveChangesAsync(cancellationToken);

        await _securityAudit.RecordAsync(SecurityAuditActions.PasswordResetRequested, user.Id, new { requestedFromIp = job.IpAddress }, cancellationToken);
        return true;
    }
}
