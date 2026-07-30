namespace SIGH.Application.Options;

public class StorageOptions
{
    public const string SectionName = "StorageOptions";

    public string RootPath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 10485760; // 10 MB
}
