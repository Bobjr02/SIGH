namespace SIGH.Application.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
