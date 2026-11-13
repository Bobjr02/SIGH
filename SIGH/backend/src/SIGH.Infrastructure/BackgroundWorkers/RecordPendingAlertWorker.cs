using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Options;

namespace SIGH.Infrastructure.BackgroundWorkers;

/// <summary>
/// Roda o aviso de medidas pendentes há mais de N dias. O processador garante um aviso por dia por
/// aprovador, então o intervalo (BackgroundWorkerOptions) só define quão cedo no dia o aviso sai.
/// </summary>
public sealed class RecordPendingAlertWorker : BackgroundService
{
    private readonly ILogger<RecordPendingAlertWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundWorkerOptions _options;

    public RecordPendingAlertWorker(ILogger<RecordPendingAlertWorker> logger, IServiceScopeFactory scopeFactory, IOptions<BackgroundWorkerOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notified = await scope.ServiceProvider.GetRequiredService<IRecordPendingAlertProcessor>().ProcessAsync(stoppingToken);
                if (notified > 0)
                    _logger.LogInformation("[BackgroundWorker] Aviso de medidas pendentes enviado a {Count} aprovador(es).", notified);

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(60, _options.CheckIntervalInSeconds)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundWorker] Erro no aviso de medidas pendentes.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
            }
        }
    }
}
