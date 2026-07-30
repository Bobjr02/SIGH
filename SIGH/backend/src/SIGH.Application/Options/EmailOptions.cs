namespace SIGH.Application.Options;

public class EmailOptions
{
    public const string SectionName = "EmailOptions";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}
