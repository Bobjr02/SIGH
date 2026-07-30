namespace SIGH.Application.Interfaces;

public interface IBackgroundJobScheduler
{
    Task ScheduleJobAsync(string jobName, Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default);
    Task EnqueueRecurringJobAsync(string jobName, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    bool CancelJob(string jobId);
}
