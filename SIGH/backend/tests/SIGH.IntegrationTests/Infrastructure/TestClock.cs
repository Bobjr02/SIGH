using SIGH.Application.Interfaces;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>Relógio da API nos testes: tempo real mais um deslocamento ajustável.</summary>
public sealed class TestClock : IDateTimeProvider
{
    private TimeSpan _offset = TimeSpan.Zero;

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + _offset;

    public void Advance(TimeSpan by) => _offset += by;

    public void Reset() => _offset = TimeSpan.Zero;
}
