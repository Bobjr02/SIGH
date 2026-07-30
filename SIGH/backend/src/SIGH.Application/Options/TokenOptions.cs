namespace SIGH.Application.Options;

public class TokenOptions
{
    public const string SectionName = "TokenOptions";

    public int RefreshTokenExpirationInDays { get; set; } = 7;
}
