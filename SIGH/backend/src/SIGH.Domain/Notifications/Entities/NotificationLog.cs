namespace SIGH.Domain.Notifications.Entities;

using SIGH.Domain.Common;

public class NotificationLog : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public string SourceEntity { get; private set; } = string.Empty;
    public Guid SourceEntityId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid RecipientUserId { get; private set; }
    public DateTimeOffset LastSentAt { get; private set; }
    public int RemindersSentCount { get; private set; }
    public int EscalationLevel { get; private set; }
    public Guid? NotificationId { get; private set; }
    public string? Metadata { get; private set; }

    // EF Core constructor
    protected NotificationLog() : base() { }

    public NotificationLog(
        Guid companyId,
        string sourceEntity,
        Guid sourceEntityId,
        string eventType,
        Guid recipientUserId,
        DateTimeOffset sentAt,
        int remindersSentCount = 1,
        int escalationLevel = 0,
        Guid? notificationId = null,
        string? metadata = null) : base()
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId é obrigatório.", nameof(companyId));

        if (string.IsNullOrWhiteSpace(sourceEntity))
            throw new ArgumentException("SourceEntity é obrigatório.", nameof(sourceEntity));

        if (sourceEntityId == Guid.Empty)
            throw new ArgumentException("SourceEntityId é obrigatório.", nameof(sourceEntityId));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType é obrigatório.", nameof(eventType));

        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("RecipientUserId é obrigatório.", nameof(recipientUserId));

        CompanyId = companyId;
        SourceEntity = sourceEntity.Trim();
        SourceEntityId = sourceEntityId;
        EventType = eventType.Trim();
        RecipientUserId = recipientUserId;
        LastSentAt = sentAt;
        RemindersSentCount = Math.Max(0, remindersSentCount);
        EscalationLevel = Math.Max(0, escalationLevel);
        NotificationId = notificationId;
        Metadata = metadata;
    }

    public static NotificationLog Create(
        Guid companyId,
        string sourceEntity,
        Guid sourceEntityId,
        string eventType,
        Guid recipientUserId,
        DateTimeOffset sentAt,
        int remindersSentCount = 1,
        int escalationLevel = 0,
        Guid? notificationId = null,
        string? metadata = null)
    {
        return new NotificationLog(
            companyId,
            sourceEntity,
            sourceEntityId,
            eventType,
            recipientUserId,
            sentAt,
            remindersSentCount,
            escalationLevel,
            notificationId,
            metadata);
    }

    public void RecordReminderSent(DateTimeOffset sentAt, Guid? notificationId = null)
    {
        RemindersSentCount++;
        LastSentAt = sentAt;
        if (notificationId.HasValue)
        {
            NotificationId = notificationId;
        }
        UpdatedAt = sentAt;
    }

    public void RecordEscalation(int newLevel, DateTimeOffset sentAt, Guid? notificationId = null)
    {
        EscalationLevel = newLevel;
        LastSentAt = sentAt;
        if (notificationId.HasValue)
        {
            NotificationId = notificationId;
        }
        UpdatedAt = sentAt;
    }
}
