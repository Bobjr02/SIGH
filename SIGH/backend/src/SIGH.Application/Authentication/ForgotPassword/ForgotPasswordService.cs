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

public class ForgotPasswordService : IForgotPasswordService
{
    public const string GenericMessage =
        "Se o e-mail informado estiver cadastrado, você receberá as instruções para redefinir a senha.";

    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordTokenService _tokens;
    private readonly IEmailOutbox _outbox;
    private readonly ISecurityAuditLogger _securityAudit;
    private readonly AppOptions _appOptions;

    public ForgotPasswordService(
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

    /// <summary>
    /// Sempre devolve a mesma resposta, exista ou não o e-mail (sem enumeração de usuários). O e-mail
    /// vai pela outbox: a requisição não espera o SMTP.
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = string.IsNullOrEmpty(email) ? null : await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Contas inativas ou suspensas não recebem link (o administrador precisa reativá-las).
        if (user is not null && user.Status is not (UserStatus.Inactive or UserStatus.Suspended))
        {
            var rawToken = await _tokens.IssueAsync(user.Id, PasswordTokenPurpose.PasswordReset, cancellationToken);
            var link = _appOptions.BuildUrl($"/reset-password?token={Uri.EscapeDataString(rawToken)}");
            var minutes = (int)_tokens.LifetimeOf(PasswordTokenPurpose.PasswordReset).TotalMinutes;

            _outbox.Enqueue(user.Email, EmailTemplates.PasswordReset(user.FullName, link, minutes));
            await _context.SaveChangesAsync(cancellationToken);

            await _securityAudit.RecordAsync(SecurityAuditActions.PasswordResetRequested, user.Id, null, cancellationToken);
        }

        return new ForgotPasswordResponse(true, GenericMessage);
    }
}
