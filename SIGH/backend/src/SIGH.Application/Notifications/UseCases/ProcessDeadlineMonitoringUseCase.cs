namespace SIGH.Application.Notifications.UseCases;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SIGH.Application.Common.Models;
using SIGH.Application.Notifications.DTOs;

public class ProcessDeadlineMonitoringUseCase : IProcessDeadlineMonitoringUseCase
{
    private readonly IProcessRecurringRemindersUseCase _remindersUseCase;
    private readonly IProcessDeadlineEscalationsUseCase _escalationsUseCase;
    private readonly ILogger<ProcessDeadlineMonitoringUseCase> _logger;

    public ProcessDeadlineMonitoringUseCase(
        IProcessRecurringRemindersUseCase remindersUseCase,
        IProcessDeadlineEscalationsUseCase escalationsUseCase,
        ILogger<ProcessDeadlineMonitoringUseCase> logger)
    {
        _remindersUseCase = remindersUseCase;
        _escalationsUseCase = escalationsUseCase;
        _logger = logger;
    }

    public async Task<Result<DeadlineProcessingResultDto>> ExecuteAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var combinedResult = new DeadlineProcessingResultDto();

        _logger.LogInformation("[ProcessDeadlineMonitoring] Orquestrando monitoramento completo de prazos (Lembretes + Escalonamento).");

        // 1. Lembretes recorrentes
        var reminderResult = await _remindersUseCase.ExecuteAsync(companyId, cancellationToken);
        if (reminderResult.Success && reminderResult.Data != null)
        {
            combinedResult.TotalItemsProcessed += reminderResult.Data.TotalItemsProcessed;
            combinedResult.RemindersGenerated += reminderResult.Data.RemindersGenerated;
            combinedResult.ItemsSkippedDueToDuplicity += reminderResult.Data.ItemsSkippedDueToDuplicity;
            combinedResult.FailedItemsCount += reminderResult.Data.FailedItemsCount;
            combinedResult.ExecutionLogs.AddRange(reminderResult.Data.ExecutionLogs);
        }

        // 2. Escalonamento automático
        var escalationResult = await _escalationsUseCase.ExecuteAsync(companyId, cancellationToken);
        if (escalationResult.Success && escalationResult.Data != null)
        {
            combinedResult.TotalItemsProcessed += escalationResult.Data.TotalItemsProcessed;
            combinedResult.EscalationsExecuted += escalationResult.Data.EscalationsExecuted;
            combinedResult.ItemsSkippedDueToDuplicity += escalationResult.Data.ItemsSkippedDueToDuplicity;
            combinedResult.FailedItemsCount += escalationResult.Data.FailedItemsCount;
            combinedResult.ExecutionLogs.AddRange(escalationResult.Data.ExecutionLogs);
        }

        stopwatch.Stop();
        combinedResult.ExecutionDurationMilliseconds = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "[ProcessDeadlineMonitoring] Concluído em {Duration}ms. Lembretes: {Reminders}, Escalonamentos: {Escalations}, Ignorados por duplicidade: {Duplicates}, Falhas: {Failures}.",
            combinedResult.ExecutionDurationMilliseconds,
            combinedResult.RemindersGenerated,
            combinedResult.EscalationsExecuted,
            combinedResult.ItemsSkippedDueToDuplicity,
            combinedResult.FailedItemsCount);

        return Result<DeadlineProcessingResultDto>.Ok(combinedResult);
    }
}
