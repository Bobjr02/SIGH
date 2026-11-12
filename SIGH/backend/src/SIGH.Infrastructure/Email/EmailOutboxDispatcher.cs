using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Email;
using SIGH.Application.Options;

namespace SIGH.Infrastructure.Email;

/// <summary>Worker que esvazia a outbox periodicamente (Email:Outbox:PollIntervalSeconds).</summary>
public sealed class EmailOutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailOutboxDispatcher> _logger;

    public EmailOutboxDispatcher(IServiceScopeFactory scopeFactory, IOptions<EmailOptions> options, ILogger<EmailOutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            _logger.LogWarning("Email:Enabled=false: usando NullEmailService. Nenhum e-mail será enviado (as mensagens da outbox serão descartadas como enviadas).");

        if (!_options.Outbox.DispatcherEnabled)
        {
            _logger.LogWarning("Email:Outbox:DispatcherEnabled=false: o worker de envio de e-mails está desligado.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.Outbox.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no worker da outbox de e-mails.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Um ciclo: processa um lote de mensagens vencidas.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>();
        return await processor.ProcessDueAsync(cancellationToken);
    }
}
