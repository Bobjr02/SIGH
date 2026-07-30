namespace SIGH.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using SIGH.Application.Interfaces;

public class BackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<BackgroundJobScheduler> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _scheduledJobs = new();

    public BackgroundJobScheduler(ILogger<BackgroundJobScheduler> logger)
    {
        _logger = logger;
    }

    public Task ScheduleJobAsync(string jobName, Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[BackgroundJobScheduler] Agendando job '{JobName}' com atraso de {Delay}.", jobName, delay);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scheduledJobs[jobName] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("[BackgroundJobScheduler] Executando job agendado '{JobName}'.", jobName);
                    await action(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[BackgroundJobScheduler] Job '{JobName}' foi cancelado.", jobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobScheduler] Erro durante execução do job '{JobName}'.", jobName);
            }
        }, cts.Token);

        return Task.CompletedTask;
    }

    public Task EnqueueRecurringJobAsync(string jobName, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[BackgroundJobScheduler] Registrando job recorrente '{JobName}'.", jobName);
        return Task.CompletedTask;
    }

    public bool CancelJob(string jobId)
    {
        if (_scheduledJobs.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            _scheduledJobs.Remove(jobId);
            _logger.LogInformation("[BackgroundJobScheduler] Job '{JobId}' cancelado com sucesso.", jobId);
            return true;
        }

        return false;
    }
}
