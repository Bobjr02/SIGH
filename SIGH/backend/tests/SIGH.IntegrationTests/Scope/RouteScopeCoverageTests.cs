using FluentAssertions;
using Xunit;

namespace SIGH.IntegrationTests.Scope;

/// <summary>
/// Toda rota GET da API está classificada como "com escopo" ou "não se aplica", e toda rota com escopo
/// tem um caso em <see cref="ScopeEnforcementTests"/> (gestor da USGA132 não enxerga a USGA131).
/// </summary>
public class RouteScopeCoverageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RouteScopeCoverageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void EveryGetRoute_IsClassified()
    {
        _ = _factory.CreateClient(); // sobe o host
        var routes = GetRoutes.All(_factory);
        routes.Should().HaveCountGreaterThan(30);

        var unclassified = routes
            .Where(r => !RouteScopeClassification.Scoped.Contains(r) && !RouteScopeClassification.NotApplicable.ContainsKey(r))
            .ToList();
        unclassified.Should().BeEmpty("toda rota GET precisa estar em RouteScopeClassification (com escopo ou não se aplica)");

        RouteScopeClassification.Scoped.Should().NotIntersectWith(RouteScopeClassification.NotApplicable.Keys);

        // Classificações de rotas que não existem mais também falham (a lista não pode envelhecer).
        RouteScopeClassification.Scoped.Concat(RouteScopeClassification.NotApplicable.Keys)
            .Should().BeSubsetOf(routes);
    }

    [Fact]
    public void EveryScopedRoute_HasAnEnforcementTest()
    {
        ScopeEnforcementTests.Cases.Keys.Should().BeEquivalentTo(RouteScopeClassification.Scoped,
            "cada rota com escopo precisa de um teste de integração em ScopeEnforcementTests");
    }
}
