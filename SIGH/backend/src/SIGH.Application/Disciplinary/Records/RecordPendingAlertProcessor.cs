using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Notifications.Entities;

namespace SIGH.Application.Disciplinary.Records;

public interface IRecordPendingAlertProcessor
{
    /// <returns>Quantos aprovadores foram avisados nesta execução.</returns>
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Medidas aguardando aprovação há mais de Records:PendingApprovalAlertDays (padrão 3) dias: cada aprovador
/// recebe UM aviso por dia (resumo com a quantidade), controlado por NotificationLog. Pode rodar quantas
/// vezes quiser no dia. Não conta as medidas que o próprio aprovador registrou (ele não pode aprová-las).
/// </summary>
public sealed class RecordPendingAlertProcessor : IRecordPendingAlertProcessor
{
    public const string SourceEntity = "DisciplinaryRecords";

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly RecordNotifier _notifier;
    private readonly RecordsOptions _options;

    public RecordPendingAlertProcessor(IApplicationDbContext db, IDateTimeProvider clock, RecordNotifier notifier, IOptions<RecordsOptions> options)
    {
        _db = db;
        _clock = clock;
        _notifier = notifier;
        _options = options.Value;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var threshold = now.AddDays(-Math.Max(0, _options.PendingApprovalAlertDays));

        var overdue = await _db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.Kind == DisciplinaryCaseKind.Record && c.Status == DisciplinaryCaseStatus.AwaitingDecision)
            .SelectMany(c => c.Decisions
                .Where(d => d.Status == DecisionStatus.PendingApproval && d.DecidedAt <= threshold)
                .Select(d => new { c.CompanyId, c.OpenedByUserId, d.DecidedByUserId }))
            .ToListAsync(cancellationToken);
        if (overdue.Count == 0)
            return 0;

        var approvers = await RecordNotifier.Approvers(_db, now)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        var today = BusinessCalendar.Today(now);
        var notified = 0;
        foreach (var approver in approvers)
        {
            var mine = overdue.Where(o => o.OpenedByUserId != approver.Id && o.DecidedByUserId != approver.Id).ToList();
            if (mine.Count == 0)
                continue;

            var log = await _db.NotificationLogs.FirstOrDefaultAsync(l => l.SourceEntity == SourceEntity
                                                                          && l.SourceEntityId == approver.Id
                                                                          && l.EventType == RecordNotifier.PendingDigestType, cancellationToken);
            if (log is not null && BusinessCalendar.DateOf(log.LastSentAt) >= today)
                continue;

            var companyId = mine[0].CompanyId;
            var message = mine.Count == 1
                ? $"1 medida aguarda aprovação há mais de {_options.PendingApprovalAlertDays} dias."
                : $"{mine.Count} medidas aguardam aprovação há mais de {_options.PendingApprovalAlertDays} dias.";
            _notifier.Notify(companyId, approver.Id, approver.FullName, approver.Email, "Medidas pendentes de aprovação", message, RecordNotifier.PendingDigestType, recordId: null);

            if (log is null)
                _db.NotificationLogs.Add(new NotificationLog(companyId, SourceEntity, approver.Id, RecordNotifier.PendingDigestType, approver.Id, now));
            else
                log.RecordReminderSent(now);
            notified++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return notified;
    }
}
