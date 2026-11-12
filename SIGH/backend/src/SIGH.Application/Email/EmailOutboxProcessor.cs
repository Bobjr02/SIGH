using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;

namespace SIGH.Application.Email;

/// <summary>Acesso da outbox ao banco, usado só pelo processador.</summary>
public interface IEmailOutboxStore
{
    Task<IReadOnlyList<EmailOutboxMessage>> GetDueAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Envia as mensagens pendentes cujo horário chegou. Falha numa mensagem não afeta as outras:
/// o erro é registrado e a próxima tentativa é agendada com backoff exponencial, até
/// Email:Outbox:MaxAttempts (padrão 5); depois disso a mensagem fica como Failed.
/// </summary>
public sealed class EmailOutboxProcessor
{
    private readonly IEmailOutboxStore _store;
    private readonly IEmailSender _sender;
    private readonly IDateTimeProvider _clock;
    private readonly EmailOutboxOptions _options;
    private readonly ILogger<EmailOutboxProcessor> _logger;

    public EmailOutboxProcessor(
        IEmailOutboxStore store,
        IEmailSender sender,
        IDateTimeProvider clock,
        IOptions<EmailOptions> options,
        ILogger<EmailOutboxProcessor> logger)
    {
        _store = store;
        _sender = sender;
        _clock = clock;
        _options = options.Value.Outbox;
        _logger = logger;
    }

    /// <summary>Processa um lote. Devolve quantas mensagens foram enviadas com sucesso.</summary>
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var due = await _store.GetDueAsync(_clock.UtcNow, Math.Max(1, _options.BatchSize), cancellationToken);
        var sent = 0;

        foreach (var message in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _sender.SendAsync(new OutgoingEmail(message.To, message.Subject, message.HtmlBody, message.TextBody), cancellationToken);
                message.MarkSent(_clock.UtcNow);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var maxAttempts = Math.Max(1, _options.MaxAttempts);
                message.MarkAttemptFailed($"{ex.GetType().Name}: {ex.Message}", _clock.UtcNow, maxAttempts, RetryDelay(message.Attempts + 1));

                if (message.Status == EmailOutboxStatus.Failed)
                    _logger.LogError(ex, "E-mail {MessageId} ({Template}) falhou definitivamente após {Attempts} tentativas.", message.Id, message.Template, message.Attempts);
                else
                    _logger.LogWarning(ex, "Falha ao enviar e-mail {MessageId} ({Template}), tentativa {Attempts}/{Max}; nova tentativa em {Next}.",
                        message.Id, message.Template, message.Attempts, maxAttempts, message.NextAttemptAt);
            }

            // Grava a cada mensagem: um erro adiante não desfaz o que já foi enviado.
            await _store.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    /// <summary>Espera antes da tentativa de número <paramref name="attemptNumber"/> + 1 (30 s, 60 s, 120 s, ...).</summary>
    public TimeSpan RetryDelay(int attemptNumber)
    {
        var initial = Math.Max(1, _options.InitialRetryDelaySeconds);
        var seconds = initial * Math.Pow(2, Math.Max(0, attemptNumber - 1));
        return TimeSpan.FromSeconds(Math.Min(seconds, Math.Max(initial, _options.MaxRetryDelaySeconds)));
    }
}
