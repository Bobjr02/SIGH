namespace SIGH.Application.Options;

public class BackgroundWorkerOptions
{
    public const string SectionName = "BackgroundWorkerOptions";

    public bool Enabled { get; set; } = true;
    public int CheckIntervalInSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
    public int MaxRetryAttempts { get; set; } = 3;
}
