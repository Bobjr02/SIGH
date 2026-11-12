using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SIGH.Api.Contracts.Auth;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.Logout;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// O administrador criado pelo DatabaseSeeder nasce com MustChangePassword = true:
/// até trocar a senha, só change-password, logout e refresh podem ser usados.
/// </summary>
public class PasswordChangeEnforcementTests : IntegrationTestBase
{
    private const string NewPassword = "Nova@Senha2026!";

    public PasswordChangeEnforcementTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task PendingUser_GetUsers_Returns403WithPasswordChangeRequiredCode()
    {
        var login = await LoginAsync(CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);
        login.MustChangePassword.Should().BeTrue();

        var token = ReadToken(login.AccessToken);
        token.Claims.Should().ContainSingle(c => c.Type == SighClaimTypes.PasswordChangeRequired).Which.Value.Should().Be("true");
        token.Claims.Should().NotContain(c => c.Type == "cpf");

        using var client = CreateClient(login.AccessToken);

        foreach (var path in new[] { "/api/v1/users", "/api/v1/notifications" })
        {
            var response = await client.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, path);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("status").GetInt32().Should().Be(403);
            problem.RootElement.GetProperty("code").GetString().Should().Be("PASSWORD_CHANGE_REQUIRED");
        }
    }

    [Fact]
    public async Task PendingUser_CanRefreshAndLogout()
    {
        using var client = Factory.CreateClient();
        var (_, _) = await AuthTestHelpers.LoginAsync(client, CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await refreshResponse.Content.ReadFromJsonAsync<Result<RefreshHttpResponse>>())!.Data!;

        // A senha ainda não foi trocada: o token renovado continua restrito.
        ReadToken(refreshed.AccessToken).Claims.Should().Contain(c => c.Type == SighClaimTypes.PasswordChangeRequired);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await logoutResponse.Content.ReadFromJsonAsync<Result<LogoutResponse>>())!.Success.Should().BeTrue();
        RefreshCookie.From(logoutResponse)!.IsDeletion.Should().BeTrue();
    }

    [Fact]
    public async Task PendingUser_ChangesPassword_ThenReceivesUnrestrictedTokensAndCanAccessApi()
    {
        using var client = Factory.CreateClient();
        var (login, oldCookie) = await AuthTestHelpers.LoginAsync(client, CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);
        login.MustChangePassword.Should().BeTrue();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        // Troca de senha (liberada para o usuário pendente).
        var changeResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordHttpRequest(CustomWebApplicationFactory.SeedAdminPassword, NewPassword));

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var changeJson = await changeResponse.Content.ReadAsStringAsync();
        changeJson.Should().NotContain("refreshToken");
        var changed = JsonSerializer.Deserialize<Result<ChangePasswordHttpResponse>>(changeJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Data!;
        changed.Success.Should().BeTrue();
        changed.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Novo refresh token no cookie, com os mesmos atributos.
        var newCookie = RefreshCookie.From(changeResponse);
        newCookie.Should().NotBeNull();
        newCookie!.Value.Should().NotBe(oldCookie.Value);
        newCookie.Has("httponly").Should().BeTrue();
        newCookie.Get("path").Should().Be("/api/v1/auth");

        var newToken = ReadToken(changed.AccessToken);
        newToken.Claims.Should().NotContain(c => c.Type == SighClaimTypes.PasswordChangeRequired);
        newToken.Claims.Should().NotContain(c => c.Type == "cpf");

        // Com o novo token, o acesso funciona.
        using var apiClient = CreateClient(changed.AccessToken);
        (await apiClient.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.OK);

        // O token antigo (com a claim) continua restrito até expirar.
        using var pendingClient = CreateClient(login.AccessToken);
        (await pendingClient.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // O refresh token emitido antes da troca foi revogado...
        using var replayClient = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, refreshToken: oldCookie.Value);
        (await replayClient.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Novo login com a nova senha já não exige troca.
        using var reloginClient = Factory.CreateClient();
        var (relogin, _) = await AuthTestHelpers.LoginAsync(reloginClient, CustomWebApplicationFactory.SeedAdminEmail, NewPassword);
        relogin.MustChangePassword.Should().BeFalse();
        ReadToken(relogin.AccessToken).Claims.Should().NotContain(c => c.Type == SighClaimTypes.PasswordChangeRequired);
    }

    [Fact]
    public async Task ChangePassword_NewCookieRefreshes()
    {
        using var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        (await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new ChangePasswordHttpRequest(CustomWebApplicationFactory.SeedAdminPassword, NewPassword)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // O cookie jar agora tem o cookie emitido pelo change-password.
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await refresh.Content.ReadFromJsonAsync<Result<RefreshHttpResponse>>())!.Data!;
        ReadToken(refreshed.AccessToken).Claims.Should().NotContain(c => c.Type == SighClaimTypes.PasswordChangeRequired);
    }

    private async Task<LoginHttpResponse> LoginAsync(string email, string password)
    {
        using var client = Factory.CreateClient();
        return (await AuthTestHelpers.LoginAsync(client, email, password)).Body;
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static JwtSecurityToken ReadToken(string accessToken) => new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
}
