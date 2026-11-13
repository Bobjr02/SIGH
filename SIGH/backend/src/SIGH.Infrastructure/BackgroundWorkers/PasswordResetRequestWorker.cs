using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIGH.Application.Authentication.ForgotPassword;

namespace SIGH.Infrastructure.BackgroundWorkers;

/// <summary>Consome a fila de "esqueci a senha" (um pedido por vez, cada um no seu escopo).</summary>
public sealed class PasswordResetRequestWorker : BackgroundService
{
    private readonly IPasswordResetRequestQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasswordResetRequestWorker> _logger;

    public PasswordResetRequestWorker(IPasswordResetRequestQueue queue, IServiceScopeFactory scopeFactory, ILogger<PasswordResetRequestWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<PasswordResetRequestProcessor>().ProcessAsync(job, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Sem o e-mail no log: pode ser de terceiros.
                    _logger.LogError(ex, "Falha ao processar um pedido de redefinição de senha.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // encerrando
        }
    }
}
