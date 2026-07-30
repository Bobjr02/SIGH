using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.Application.Notifications.DTOs;
using Xunit;

namespace SIGH.IntegrationTests;

public class NotificationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotificationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var loginRequest = new LoginRequest("admin@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);
    }

    [Fact]
    public async Task GetNotifications_WhenAuthenticated_Returns200OKWithPagedResult()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/notifications?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<NotificationDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetUnreadNotifications_WhenAuthenticated_Returns200OKWithList()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/notifications/unread");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<IEnumerable<NotificationDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task NotificationsEndpoint_WhenUnauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var factory = new CustomWebApplicationFactory();
        var anonymousClient = factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("/api/v1/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NotificationsEndpoint_WhenUserLacksPermission_Returns403Forbidden()
    {
        // Arrange
        var loginRequest = new LoginRequest("unprivileged@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        var unprivilegedClient = new CustomWebApplicationFactory().CreateClient();
        unprivilegedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        // Act
        var response = await unprivilegedClient.GetAsync("/api/v1/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
