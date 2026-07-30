namespace SIGH.Infrastructure.BackgroundWorkers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Notifications.UseCases;
using SIGH.Application.Options;

public class DisciplinaryDeadlineBackgroundWorker : BackgroundService
{
    private readonly ILogger<DisciplinaryDeadlineBackgroundWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundWorkerOptions _options;

    public DisciplinaryDeadlineBackgroundWorker(
        ILogger<DisciplinaryDeadlineBackgroundWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundWorkerOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("[BackgroundWorker] DisciplinaryDeadlineBackgroundWorker está desativado nas configurações (Options.Enabled = false).");
            return;
        }

        _logger.LogInformation("[BackgroundWorker] DisciplinaryDeadlineBackgroundWorker iniciado com intervalo de {Interval} segundos.", _options.CheckIntervalInSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("[BackgroundWorker] Iniciando ciclo de monitoramento de prazos em {Time}.", DateTime.UtcNow);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var monitoringUseCase = scope.ServiceProvider.GetRequiredService<IProcessDeadlineMonitoringUseCase>();
                    var result = await monitoringUseCase.ExecuteAsync(companyId: null, cancellationToken: stoppingToken);

                    if (result.Success && result.Data != null)
                    {
                        var data = result.Data;
                        _logger.LogInformation(
                            "[BackgroundWorker] Ciclo de monitoramento de prazos finalizado em {Duration:N0}ms. " +
                            "Itens processados: {Total}, Lembretes gerados: {Reminders}, Escalonamentos executados: {Escalations}, " +
                            "Ignorados por duplicidade: {Duplicates}, Falhas: {Failures}.",
                            data.ExecutionDurationMilliseconds,
                            data.TotalItemsProcessed,
                            data.RemindersGenerated,
                            data.EscalationsExecuted,
                            data.ItemsSkippedDueToDuplicity,
                            data.FailedItemsCount);
                    }
                    else
                    {
                        _logger.LogWarning("[BackgroundWorker] Falha durante o ciclo de monitoramento de prazos: {Message} (Erro: {ErrorCode})", result.Message, result.ErrorCode);
                    }
                }

                var interval = TimeSpan.FromSeconds(Math.Max(5, _options.CheckIntervalInSeconds));
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[BackgroundWorker] DisciplinaryDeadlineBackgroundWorker interrompido via CancellationToken.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundWorker] Erro não tratado durante o processamento de prazos do worker.");
            }
        }

        _logger.LogInformation("[BackgroundWorker] DisciplinaryDeadlineBackgroundWorker finalizado.");
    }
}
