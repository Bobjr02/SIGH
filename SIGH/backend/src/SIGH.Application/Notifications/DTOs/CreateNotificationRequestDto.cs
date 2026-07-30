namespace SIGH.Application.Notifications.DTOs;

public class CreateNotificationRequestDto
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Priority { get; set; } = "Medium";
    public DateTimeOffset? DueDate { get; set; }
    public string? Metadata { get; set; }
}
