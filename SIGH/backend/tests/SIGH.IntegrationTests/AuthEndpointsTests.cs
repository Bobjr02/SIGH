using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SIGH.Api.Contracts.Auth;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Login, refresh e logout com o refresh token no cookie HttpOnly sigh_rt.
/// </summary>
public class AuthEndpointsTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@sigh.com";

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessTokenInBodyAndRefreshTokenOnlyInCookie()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(AdminEmail, CustomWebApplicationFactory.DefaultPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        data.TryGetProperty("refreshToken", out _).Should().BeFalse("o refresh token não pode voltar no corpo");
        data.GetProperty("user").TryGetProperty("cpf", out _).Should().BeFalse("respostas de autenticação não expõem CPF");

        var cookie = RefreshCookie.From(response);
        cookie.Should().NotBeNull();
        cookie!.Value.Should().NotBeNullOrEmpty();
        json.Should().NotContain(cookie.Value);
        cookie.Has("httponly").Should().BeTrue();
        cookie.Has("secure").Should().BeTrue();
        cookie.Get("samesite").Should().BeEquivalentTo("strict");
        cookie.Get("path").Should().Be("/api/v1/auth");

        // Expiração igual à do refresh token (TokenOptions: 7 dias por padrão).
        DateTimeOffset.Parse(cookie.Get("expires")!).Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns400BadRequestWithoutCookie()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(AdminEmail, "WrongPassword123!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        RefreshCookie.From(response).Should().BeNull();
    }

    [Fact]
    public async Task Refresh_WithCookie_IgnoresBodyRotatesCookieAndReturnsNewAccessToken()
    {
        using var client = Factory.CreateClient();
        var (login, loginCookie) = await AuthTestHelpers.LoginAsync(client, AdminEmail, CustomWebApplicationFactory.DefaultPassword);

        // O corpo é ignorado: um token forjado no corpo não interfere.
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "forged-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("refreshToken");

        var refreshed = JsonSerializer.Deserialize<Result<RefreshHttpResponse>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Data!;
        refreshed.AccessToken.Should().NotBeNullOrEmpty();

        var rotated = RefreshCookie.From(response);
        rotated.Should().NotBeNull();
        rotated!.Value.Should().NotBe(loginCookie.Value);
        rotated.Has("httponly").Should().BeTrue();
        rotated.Get("path").Should().Be("/api/v1/auth");
        login.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401EvenWithTokenInBody()
    {
        using var loginClient = Factory.CreateClient();
        var (_, cookie) = await AuthTestHelpers.LoginAsync(loginClient, AdminEmail, CustomWebApplicationFactory.DefaultPassword);

        using var client = AuthTestHelpers.CreateClientWithoutCookieJar(Factory);
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = cookie.Value });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithRotatedCookie_AfterGraceWindow_IsRejectedAndCookieIsDeleted()
    {
        using var client = Factory.CreateClient();
        var (_, firstCookie) = await AuthTestHelpers.LoginAsync(client, AdminEmail, CustomWebApplicationFactory.DefaultPassword);
        (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Dentro da janela de tolerância (Auth:RefreshTokenReuseGraceSeconds) o cookie antigo ainda vale.
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));

        using var replayClient = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, refreshToken: firstCookie.Value);
        var response = await replayClient.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        RefreshCookie.From(response)!.IsDeletion.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_RevokesRefreshTokenFromCookieAndDeletesCookie()
    {
        using var client = Factory.CreateClient();
        var (login, cookie) = await AuthTestHelpers.LoginAsync(client, AdminEmail, CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletion = RefreshCookie.From(response);
        deletion.Should().NotBeNull();
        deletion!.IsDeletion.Should().BeTrue();
        deletion.Get("path").Should().Be("/api/v1/auth");

        // O cookie jar descartou o cookie: refresh sem cookie.
        (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // E o token antigo foi revogado no servidor.
        using var replayClient = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, refreshToken: cookie.Value);
        (await replayClient.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
