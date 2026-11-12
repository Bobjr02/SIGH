using Microsoft.Extensions.Logging;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Email;

/// <summary>
/// Usado com Email:Enabled=false: não envia nada (as mensagens da outbox são marcadas como enviadas).
/// O startup registra um aviso quando este serviço está ativo.
/// </summary>
public sealed class NullEmailService : IEmailSender
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Email:Enabled=false: e-mail \"{Subject}\" não enviado.", email.Subject);
        return Task.CompletedTask;
    }
}
