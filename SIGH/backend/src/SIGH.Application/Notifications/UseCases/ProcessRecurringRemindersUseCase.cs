using SIGH.Application.Common.Interfaces;
namespace SIGH.Application.Notifications.UseCases;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Options;
using SIGH.Domain.Notifications.Entities;

public class ProcessRecurringRemindersUseCase : IProcessRecurringRemindersUseCase
{
    private readonly IDeadlineMonitoringQueryRepository _queryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IApplicationDbContext _context;
    private readonly NotificationReminderOptions _reminderOptions;
    private readonly ILogger<ProcessRecurringRemindersUseCase> _logger;

    public ProcessRecurringRemindersUseCase(
        IDeadlineMonitoringQueryRepository queryRepository,
        INotificationRepository notificationRepository,
        IDateTimeProvider dateTimeProvider,
        IApplicationDbContext context,
        IOptions<NotificationReminderOptions> reminderOptions,
        ILogger<ProcessRecurringRemindersUseCase> logger)
    {
        _queryRepository = queryRepository;
        _notificationRepository = notificationRepository;
        _dateTimeProvider = dateTimeProvider;
        _context = context;
        _reminderOptions = reminderOptions.Value;
        _logger = logger;
    }

    public async Task<Result<DeadlineProcessingResultDto>> ExecuteAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var now = _dateTimeProvider.UtcNow;
        var result = new DeadlineProcessingResultDto();

        _logger.LogInformation("[ProcessRecurringReminders] Iniciando verificação de lembretes para CompanyId: {CompanyId}.", companyId?.ToString() ?? "Todas");

        try
        {
            var items = await _queryRepository.GetActiveItemsWithDeadlinesAsync(companyId, cancellationToken);
            var alertThreshold = now.AddDays(_reminderOptions.FirstAlertDaysBeforeDue);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.TotalItemsProcessed++;

                try
                {
                    // Elegível para lembrete se o prazo estiver dentro da janela de alerta ou vencido
                    if (item.DueDate > alertThreshold)
                    {
                        continue; // Fora da janela de lembretes
                    }

                    var recipientUserId = item.ResponsibleUserId ?? item.OpenedByUserId;
                    if (recipientUserId == Guid.Empty)
                    {
                        _logger.LogWarning("[ProcessRecurringReminders] Item {ItemId} ignorado por ausência de destinatário responsável.", item.Id);
                        continue;
                    }

                    const string eventType = "DeadlineReminder";
                    var existingLog = await _notificationRepository.GetLogAsync(
                        item.CompanyId,
                        item.SourceEntity,
                        item.Id,
                        recipientUserId,
                        eventType,
                        cancellationToken);

                    if (existingLog == null)
                    {
                        // Primeiro lembrete
                        var title = $"Lembrete de Prazo: Processo {item.ReferenceNumber}";
                        var message = $"O processo disciplinar '{item.Title}' ({item.ReferenceNumber}) possui prazo previsto para {item.DueDate:dd/MM/yyyy HH:mm}. Favor verificar o andamento.";
                        var priority = item.DueDate < now ? "High" : "Medium";

                        var notification = Notification.Create(
                            item.CompanyId,
                            recipientUserId,
                            title,
                            message,
                            type: "Warning",
                            priority: priority,
                            dueDate: item.DueDate,
                            metadata: $"SourceEntity={item.SourceEntity};SourceId={item.Id}");

                        await _notificationRepository.AddAsync(notification, cancellationToken);

                        var newLog = NotificationLog.Create(
                            item.CompanyId,
                            item.SourceEntity,
                            item.Id,
                            eventType,
                            recipientUserId,
                            sentAt: now,
                            remindersSentCount: 1,
                            escalationLevel: 0,
                            notificationId: notification.Id);

                        await _notificationRepository.AddLogAsync(newLog, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        result.RemindersGenerated++;
                        result.ExecutionLogs.Add($"[Primeiro Lembrete] Item {item.ReferenceNumber} -> Usuário {recipientUserId}");
                        _logger.LogInformation("[ProcessRecurringReminders] Primeiro lembrete enviado para item {ReferenceNumber}.", item.ReferenceNumber);
                    }
                    else
                    {
                        // Lembrete recorrente
                        if (existingLog.RemindersSentCount >= _reminderOptions.MaxRemindersCount)
                        {
                            result.ItemsSkippedDueToDuplicity++;
                            _logger.LogDebug("[ProcessRecurringReminders] Item {ReferenceNumber} atingiu o limite máximo de {Max} lembretes.", item.ReferenceNumber, _reminderOptions.MaxRemindersCount);
                            continue;
                        }

                        var hoursSinceLastSent = (now - existingLog.LastSentAt).TotalHours;
                        if (hoursSinceLastSent < _reminderOptions.ReminderIntervalHours)
                        {
                            result.ItemsSkippedDueToDuplicity++;
                            _logger.LogDebug("[ProcessRecurringReminders] Item {ReferenceNumber} ignorado por duplicidade (enviado há {Hours:N1}h, mínimo {Interval}h).", item.ReferenceNumber, hoursSinceLastSent, _reminderOptions.ReminderIntervalHours);
                            continue;
                        }

                        // Envia novo lembrete recorrente
                        var reminderNum = existingLog.RemindersSentCount + 1;
                        var title = $"Lembrete Recorrente #{reminderNum}: Processo {item.ReferenceNumber}";
                        var message = $"Atenção: O prazo do processo disciplinar '{item.Title}' ({item.ReferenceNumber}) está vencendo/vencido ({item.DueDate:dd/MM/yyyy HH:mm}). Lembrete {reminderNum} de {_reminderOptions.MaxRemindersCount}.";

                        var notification = Notification.Create(
                            item.CompanyId,
                            recipientUserId,
                            title,
                            message,
                            type: "Warning",
                            priority: "High",
                            dueDate: item.DueDate,
                            metadata: $"SourceEntity={item.SourceEntity};SourceId={item.Id}");

                        await _notificationRepository.AddAsync(notification, cancellationToken);

                        existingLog.RecordReminderSent(now, notification.Id);
                        await _notificationRepository.UpdateLogAsync(existingLog, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        result.RemindersGenerated++;
                        result.ExecutionLogs.Add($"[Lembrete Recorrente #{reminderNum}] Item {item.ReferenceNumber} -> Usuário {recipientUserId}");
                        _logger.LogInformation("[ProcessRecurringReminders] Lembrete recorrente #{Num} enviado para item {ReferenceNumber}.", reminderNum, item.ReferenceNumber);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.FailedItemsCount++;
                    _logger.LogError(ex, "[ProcessRecurringReminders] Falha ao processar lembrete para item {ItemId}.", item.Id);
                    result.ExecutionLogs.Add($"[Erro] Item {item.ReferenceNumber}: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[ProcessRecurringReminders] Execução interrompida por CancellationToken.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProcessRecurringReminders] Erro geral ao processar lembretes recorrentes.");
            return Result<DeadlineProcessingResultDto>.Failure("Erro ao processar lembretes recorrentes.", "REMINDER_PROCESSING_ERROR");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionDurationMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return Result<DeadlineProcessingResultDto>.Ok(result);
    }
}
