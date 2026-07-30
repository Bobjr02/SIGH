namespace SIGH.Application.Options;

public class NotificationOptions
{
    public const string SectionName = "NotificationOptions";

    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableInAppNotifications { get; set; } = true;
    public bool EnableWhatsAppNotifications { get; set; } = false;
    public string DefaultSenderName { get; set; } = "SIGH Notificações";
    public string DefaultSenderEmail { get; set; } = "notificacoes@sigh.com.br";
}
