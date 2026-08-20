using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Common.Models;
using Xunit;

namespace SIGH.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OKAndToken()
    {
        // Arrange
        var request = new LoginRequest("admin@sigh.com", "Admin@123456");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest("admin@sigh.com", "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange - Primeiro realiza o login para obter um token válido
        var loginRequest = new LoginRequest("admin@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        var refreshRequest = new RefreshTokenRequest(loginResult!.Data!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await response.Content.ReadFromJsonAsync<Result<RefreshTokenResponse>>();
        refreshResult.Should().NotBeNull();
        refreshResult!.Success.Should().BeTrue();
        refreshResult.Data!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBe(loginResult.Data.RefreshToken);
    }
}
