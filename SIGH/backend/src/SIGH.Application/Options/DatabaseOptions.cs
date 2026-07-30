namespace SIGH.Application.Options;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseOptions";

    public string ConnectionString { get; set; } = string.Empty;
    public int MaxRetryCount { get; set; } = 3;
    public int CommandTimeoutSeconds { get; set; } = 30;
}
