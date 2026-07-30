namespace SIGH.Application.Notifications.DTOs;

public class SendNotificationRequestDto
{
    public Guid? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Channel { get; set; } = "InApp";
    public Dictionary<string, string>? Metadata { get; set; }
}
