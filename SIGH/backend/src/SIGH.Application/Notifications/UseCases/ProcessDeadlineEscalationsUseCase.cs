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

public class ProcessDeadlineEscalationsUseCase : IProcessDeadlineEscalationsUseCase
{
    private readonly IDeadlineMonitoringQueryRepository _queryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IApplicationDbContext _context;
    private readonly NotificationReminderOptions _reminderOptions;
    private readonly ILogger<ProcessDeadlineEscalationsUseCase> _logger;

    public ProcessDeadlineEscalationsUseCase(
        IDeadlineMonitoringQueryRepository queryRepository,
        INotificationRepository notificationRepository,
        IDateTimeProvider dateTimeProvider,
        IApplicationDbContext context,
        IOptions<NotificationReminderOptions> reminderOptions,
        ILogger<ProcessDeadlineEscalationsUseCase> logger)
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

        _logger.LogInformation("[ProcessDeadlineEscalations] Iniciando escalonamento automático para CompanyId: {CompanyId}.", companyId?.ToString() ?? "Todas");

        try
        {
            var items = await _queryRepository.GetActiveItemsWithDeadlinesAsync(companyId, cancellationToken);
            var escalationThreshold = now.AddDays(-_reminderOptions.EscalationDelayDays);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.TotalItemsProcessed++;

                try
                {
                    // Elegível para escalonamento se o prazo estiver vencido há mais de EscalationDelayDays
                    if (item.DueDate > escalationThreshold)
                    {
                        continue; // Ainda não atingiu o atraso mínimo para escalonamento
                    }

                    const string eventType = "DeadlineEscalation";

                    // Verifica o maior nível de escalonamento já registrado para este item
                    var logs = await _notificationRepository.GetLogsByEntityAsync(
                        item.CompanyId,
                        item.SourceEntity,
                        item.Id,
                        cancellationToken);

                    var escalationLogs = logs.Where(l => l.EventType == eventType).ToList();
                    var currentLevel = escalationLogs.Any() ? escalationLogs.Max(l => l.EscalationLevel) : 0;

                    if (currentLevel >= _reminderOptions.MaxEscalationLevels)
                    {
                        result.ItemsSkippedDueToDuplicity++;
                        _logger.LogDebug("[ProcessDeadlineEscalations] Item {ReferenceNumber} já atingiu o nível máximo de escalonamento ({MaxLevel}).", item.ReferenceNumber, _reminderOptions.MaxEscalationLevels);
                        continue;
                    }

                    var nextLevel = currentLevel + 1;

                    // Seleciona o destinatário conforme o nível de escalonamento
                    Guid recipientUserId = nextLevel switch
                    {
                        1 => item.ResponsibleUserId ?? item.OpenedByUserId,
                        2 => item.DirectSupervisorUserId ?? item.ResponsibleUserId ?? item.OpenedByUserId,
                        _ => item.HigherSupervisorUserId ?? item.DirectSupervisorUserId ?? item.OpenedByUserId
                    };

                    if (recipientUserId == Guid.Empty)
                    {
                        _logger.LogWarning("[ProcessDeadlineEscalations] Item {ReferenceNumber} ignorado por ausência de gestor/responsável para o Nível {Level}.", item.ReferenceNumber, nextLevel);
                        continue;
                    }

                    // Prevenção de duplicidade: verifica se o mesmo nível já foi enviado ao destinatário nas últimas ReminderIntervalHours
                    var existingRecipientLog = escalationLogs.FirstOrDefault(l => l.RecipientUserId == recipientUserId && l.EscalationLevel == nextLevel);
                    if (existingRecipientLog != null)
                    {
                        var hoursSinceLastSent = (now - existingRecipientLog.LastSentAt).TotalHours;
                        if (hoursSinceLastSent < _reminderOptions.ReminderIntervalHours)
                        {
                            result.ItemsSkippedDueToDuplicity++;
                            _logger.LogDebug("[ProcessDeadlineEscalations] Escalonamento Nível {Level} do item {ReferenceNumber} ignorado por duplicidade recente ({Hours:N1}h).", nextLevel, item.ReferenceNumber, hoursSinceLastSent);
                            continue;
                        }
                    }

                    var priority = nextLevel == 1 ? "High" : "Urgent";
                    var title = $"[ESCALONAMENTO NÍVEL {nextLevel}] Processo Vencido: {item.ReferenceNumber}";
                    var message = $"ATENÇÃO: O processo disciplinar '{item.Title}' ({item.ReferenceNumber}) está vencido desde {item.DueDate:dd/MM/yyyy HH:mm} e foi escalonado para o Nível {nextLevel}. Ação imediata é requerida.";

                    var notification = Notification.Create(
                        item.CompanyId,
                        recipientUserId,
                        title,
                        message,
                        type: "Warning",
                        priority: priority,
                        dueDate: item.DueDate,
                        metadata: $"SourceEntity={item.SourceEntity};SourceId={item.Id};EscalationLevel={nextLevel}");

                    await _notificationRepository.AddAsync(notification, cancellationToken);

                    if (existingRecipientLog != null)
                    {
                        existingRecipientLog.RecordEscalation(nextLevel, now, notification.Id);
                        await _notificationRepository.UpdateLogAsync(existingRecipientLog, cancellationToken);
                    }
                    else
                    {
                        var newLog = NotificationLog.Create(
                            item.CompanyId,
                            item.SourceEntity,
                            item.Id,
                            eventType,
                            recipientUserId,
                            sentAt: now,
                            remindersSentCount: 1,
                            escalationLevel: nextLevel,
                            notificationId: notification.Id);

                        await _notificationRepository.AddLogAsync(newLog, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    result.EscalationsExecuted++;
                    result.ExecutionLogs.Add($"[Escalonamento Nível {nextLevel}] Item {item.ReferenceNumber} -> Usuário {recipientUserId}");
                    _logger.LogWarning("[ProcessDeadlineEscalations] Escalonamento Nível {Level} executado para o item {ReferenceNumber}.", nextLevel, item.ReferenceNumber);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.FailedItemsCount++;
                    _logger.LogError(ex, "[ProcessDeadlineEscalations] Erro ao escalonar item {ItemId}.", item.Id);
                    result.ExecutionLogs.Add($"[Erro Escalonamento] Item {item.ReferenceNumber}: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[ProcessDeadlineEscalations] Execução interrompida por CancellationToken.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProcessDeadlineEscalations] Erro geral ao processar escalonamento de prazos.");
            return Result<DeadlineProcessingResultDto>.Failure("Erro ao processar escalonamento automático.", "ESCALATION_PROCESSING_ERROR");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionDurationMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return Result<DeadlineProcessingResultDto>.Ok(result);
    }
}
