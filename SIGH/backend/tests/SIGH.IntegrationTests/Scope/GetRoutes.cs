using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SIGH.IntegrationTests.Scope;

/// <summary>Todas as rotas GET da API, lidas do EndpointDataSource do host real.</summary>
public static class GetRoutes
{
    public static IReadOnlyList<string> All(CustomWebApplicationFactory factory)
    {
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        return source.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e =>
            {
                var methods = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
                // Sem metadado de método (ex.: health checks) a rota atende GET também.
                return methods is null || methods.Contains("GET");
            })
            .Select(e => "/" + e.RoutePattern.RawText!.TrimStart('/'))
            .Distinct()
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();
    }
}
