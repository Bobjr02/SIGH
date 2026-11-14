using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIGH.Application.Disciplinary.History;

namespace SIGH.Infrastructure.BackgroundWorkers;

/// <summary>
/// No startup, aplica History:ImportClosed (se configurado) ao estado da importação do histórico, com
/// auditoria. É o único jeito de reabrir a importação depois de encerrada pela tela.
/// </summary>
public sealed class HistoryImportStateStartup : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<HistoryImportStateStartup> _logger;

    public HistoryImportStateStartup(IServiceScopeFactory scopes, ILogger<HistoryImportStateStartup> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IHistoryImportService>().SyncStateWithConfigurationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível aplicar History:ImportClosed no startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
