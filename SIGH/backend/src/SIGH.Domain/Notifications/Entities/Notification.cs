namespace SIGH.Domain.Notifications.Entities;

using SIGH.Domain.Common;

public class Notification : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = "Info";
    public string Priority { get; private set; } = "Medium";
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsExpired { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string? Metadata { get; private set; }

    // EF Core constructor
    protected Notification() : base() { }

    public Notification(
        Guid companyId,
        Guid userId,
        string title,
        string message,
        string type = "Info",
        string priority = "Medium",
        DateTimeOffset? dueDate = null,
        string? metadata = null) : base()
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId é obrigatório.", nameof(companyId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem é obrigatória.", nameof(message));

        CompanyId = companyId;
        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Type = string.IsNullOrWhiteSpace(type) ? "Info" : type.Trim();
        Priority = string.IsNullOrWhiteSpace(priority) ? "Medium" : priority.Trim();
        DueDate = dueDate;
        Metadata = metadata;
        IsRead = false;
        ReadAt = null;
        IsExpired = false;
    }

    public static Notification Create(
        Guid companyId,
        Guid userId,
        string title,
        string message,
        string type = "Info",
        string priority = "Medium",
        DateTimeOffset? dueDate = null,
        string? metadata = null)
    {
        return new Notification(companyId, userId, title, message, type, priority, dueDate, metadata);
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = readAt;
            UpdatedAt = readAt;
        }
    }

    public void MarkAsExpired()
    {
        if (!IsExpired)
        {
            IsExpired = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
