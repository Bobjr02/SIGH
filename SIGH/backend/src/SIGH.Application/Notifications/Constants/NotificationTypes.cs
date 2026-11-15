using SIGH.Application.Disciplinary.Records;

namespace SIGH.Application.Notifications.Constants;

/// <summary>Valores de Notification.Type que chegam à tela (todos precisam de rótulo em português no front).</summary>
public static class NotificationTypes
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string RecordRegistered = RecordNotifier.RegisteredType;
    public const string RecordApproved = RecordNotifier.ApprovedType;
    public const string RecordRejected = RecordNotifier.RejectedType;
    public const string RecordPendingApproval = RecordNotifier.PendingDigestType;
}

/// <summary>Valores de Notification.Priority.</summary>
public static class NotificationPriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Urgent = "Urgent";
}
