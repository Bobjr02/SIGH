namespace SIGH.Application.Options;

public class PasswordOptions
{
    public const string SectionName = "PasswordOptions";

    public int RequiredLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public int PasswordHistoryLimit { get; set; } = 3;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutDurationInMinutes { get; set; } = 15;
}
