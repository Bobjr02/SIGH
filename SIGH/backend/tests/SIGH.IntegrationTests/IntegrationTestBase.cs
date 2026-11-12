using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Base dos testes de integração: antes de cada teste o banco é restaurado ao estado
/// inicial (Respawn + DatabaseSeeder + usuários de fixture), então nenhum teste depende
/// da ordem de execução nem dos dados deixados por outro.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected CustomWebApplicationFactory Factory { get; }

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
