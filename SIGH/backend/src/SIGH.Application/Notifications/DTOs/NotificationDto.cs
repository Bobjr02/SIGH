namespace SIGH.Application.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Priority { get; set; } = "Medium";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public bool IsExpired { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string? Metadata { get; set; }
}
