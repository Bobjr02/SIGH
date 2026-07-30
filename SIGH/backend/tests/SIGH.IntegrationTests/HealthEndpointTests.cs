using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Api.Controllers;
using SIGH.Application.Common.Models;
using Xunit;

namespace SIGH.IntegrationTests;

public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthStatus_Returns200OKAndHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<HealthStatusResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("Healthy");
        result.Data.Components.Should().ContainKey("Api");
        result.Data.Components.Should().ContainKey("Database");
    }
}
