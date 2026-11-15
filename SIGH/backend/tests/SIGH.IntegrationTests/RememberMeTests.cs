using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Interfaces;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// "Lembrar-me" no login:
/// - desmarcado: o cookie do refresh token é de SESSÃO (sem Expires/Max-Age; some ao fechar o navegador);
/// - marcado (ou omitido, clientes antigos): o cookie expira junto com o refresh token (7 dias).
/// A escolha é preservada na rotação do refresh e na troca de senha.
/// </summary>
public class RememberMeTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@sigh.com";

    public RememberMeTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<RefreshCookie> LoginAsync(bool? rememberMe)
    {
        using var client = AuthTestHelpers.CreateClientWithoutCookieJar(Factory);
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(AdminEmail, CustomWebApplicationFactory.DefaultPassword, RememberMe: rememberMe));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = RefreshCookie.From(response);
        cookie.Should().NotBeNull();
        return cookie!;
    }

    private async Task<RefreshCookie> RefreshAsync(RefreshCookie current)
    {
        using var client = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, refreshToken: current.Value);
        var response = await client.PostAsync("/api/v1/auth/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = RefreshCookie.From(response);
        rotated.Should().NotBeNull();
        rotated!.Value.Should().NotBe(current.Value);
        return rotated;
    }

    private async Task<bool> IsPersistentInDatabaseAsync(RefreshCookie cookie)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<ITokenHasher>();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var hash = hasher.HashToken(Uri.UnescapeDataString(cookie.Value));
        return await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => t.IsPersistent).SingleAsync();
    }

    private static void ShouldBeSessionCookie(RefreshCookie cookie)
    {
        cookie.Has("expires").Should().BeFalse("sem \"Lembrar-me\" o cookie é de sessão: {0}", cookie.Raw);
        cookie.Has("max-age").Should().BeFalse();
        cookie.Has("httponly").Should().BeTrue();
        cookie.Has("secure").Should().BeTrue();
        cookie.Get("samesite").Should().BeEquivalentTo("strict");
        cookie.Get("path").Should().Be("/api/v1/auth");
    }

    private static void ShouldExpireWithTheToken(RefreshCookie cookie)
    {
        cookie.Has("expires").Should().BeTrue("com \"Lembrar-me\" o cookie expira junto com o token: {0}", cookie.Raw);
        DateTimeOffset.Parse(cookie.Get("expires")!).Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task WithoutRememberMe_RefreshCookieIsASessionCookie_AndStaysSoAfterRefresh()
    {
        var login = await LoginAsync(rememberMe: false);
        ShouldBeSessionCookie(login);
        (await IsPersistentInDatabaseAsync(login)).Should().BeFalse();

        var rotated = await RefreshAsync(login);
        ShouldBeSessionCookie(rotated);
        (await IsPersistentInDatabaseAsync(rotated)).Should().BeFalse("a escolha passa para o token rotacionado");

        var again = await RefreshAsync(rotated);
        ShouldBeSessionCookie(again);
    }

    [Fact]
    public async Task WithRememberMe_RefreshCookieExpiresWithTheToken_AndStaysSoAfterRefresh()
    {
        var login = await LoginAsync(rememberMe: true);
        ShouldExpireWithTheToken(login);
        (await IsPersistentInDatabaseAsync(login)).Should().BeTrue();

        var rotated = await RefreshAsync(login);
        ShouldExpireWithTheToken(rotated);
        (await IsPersistentInDatabaseAsync(rotated)).Should().BeTrue();
    }

    [Fact]
    public async Task WithoutTheField_KeepsThePreviousBehavior()
    {
        var login = await LoginAsync(rememberMe: null);
        ShouldExpireWithTheToken(login);
    }
}
