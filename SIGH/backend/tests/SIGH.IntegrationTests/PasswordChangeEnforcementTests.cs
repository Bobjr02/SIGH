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
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
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
        var login = await LoginAsync(CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);
        using var anonymousClient = Factory.CreateClient();

        var refreshResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await refreshResponse.Content.ReadFromJsonAsync<Result<RefreshTokenResponse>>())!.Data!;

        // A senha ainda não foi trocada: o token renovado continua restrito.
        ReadToken(refreshed.AccessToken).Claims.Should().Contain(c => c.Type == SighClaimTypes.PasswordChangeRequired);

        using var client = CreateClient(refreshed.AccessToken);
        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutHttpRequest(refreshed.RefreshToken));
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await logoutResponse.Content.ReadFromJsonAsync<Result<LogoutResponse>>())!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task PendingUser_ChangesPassword_ThenReceivesUnrestrictedTokensAndCanAccessApi()
    {
        var login = await LoginAsync(CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);
        using var pendingClient = CreateClient(login.AccessToken);

        // Troca de senha (liberada para o usuário pendente).
        var changeResponse = await pendingClient.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordHttpRequest(CustomWebApplicationFactory.SeedAdminPassword, NewPassword));

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var changed = (await changeResponse.Content.ReadFromJsonAsync<Result<ChangePasswordResponse>>())!.Data!;
        changed.Success.Should().BeTrue();
        changed.AccessToken.Should().NotBeNullOrWhiteSpace();
        changed.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var newToken = ReadToken(changed.AccessToken);
        newToken.Claims.Should().NotContain(c => c.Type == SighClaimTypes.PasswordChangeRequired);
        newToken.Claims.Should().NotContain(c => c.Type == "cpf");

        // Com o novo token, o acesso funciona.
        using var client = CreateClient(changed.AccessToken);
        (await client.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.OK);

        // O refresh token emitido antes da troca foi revogado.
        using var anonymousClient = Factory.CreateClient();
        var oldRefresh = await anonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        oldRefresh.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // O token antigo (com a claim) continua restrito até expirar.
        (await pendingClient.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Novo login com a nova senha já não exige troca.
        var relogin = await LoginAsync(CustomWebApplicationFactory.SeedAdminEmail, NewPassword);
        relogin.MustChangePassword.Should().BeFalse();
        ReadToken(relogin.AccessToken).Claims.Should().NotContain(c => c.Type == SighClaimTypes.PasswordChangeRequired);
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        using var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<Result<LoginResponse>>())!.Data!;
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static JwtSecurityToken ReadToken(string accessToken) => new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
}
