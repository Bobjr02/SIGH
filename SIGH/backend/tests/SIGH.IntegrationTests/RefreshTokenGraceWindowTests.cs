using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Api.Contracts.Auth;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Common.Models;
using SIGH.Application.Security;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Duas abas renovando com o mesmo cookie: dentro de Auth:RefreshTokenReuseGraceSeconds (30 s por
/// padrão) a segunda renovação não é tratada como reuso; fora dela, a detecção de reuso continua.
/// </summary>
public class RefreshTokenGraceWindowTests : IntegrationTestBase
{
    private const string Email = "admin@sigh.com";

    public RefreshTokenGraceWindowTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TwoRefreshesWithTheSameCookie_WithinGraceWindow_BothSucceedAndNothingIsRevoked()
    {
        var (_, loginCookie) = await AuthTestHelpers.LoginAsync(Factory.CreateClient(), Email, CustomWebApplicationFactory.DefaultPassword);

        // Aba 1 renova: o token do login vira "Rotacionado".
        var first = await RefreshAsync(loginCookie.Value);
        first.Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Aba 2 chega logo depois com o MESMO cookie (já rotacionado).
        Factory.Clock.Advance(TimeSpan.FromSeconds(10));
        var second = await RefreshAsync(loginCookie.Value);
        second.Response.StatusCode.Should().Be(HttpStatusCode.OK, "dentro da janela não é reuso");
        second.Cookie.Should().NotBeNull();
        second.Cookie!.Value.Should().NotBe(first.Cookie!.Value);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var tokens = await db.RefreshTokens.Where(t => t.UserId == CustomWebApplicationFactory.AdminUserId).ToListAsync();

        tokens.Should().NotContain(t => t.ReasonRevoked == RefreshTokenRevocationReasons.ReuseDetected);
        tokens.Count(t => t.RevokedAt == null).Should().Be(2, "o sucessor da aba 1 e o token da aba 2 continuam válidos");
        (await db.UserSessions.Where(s => s.UserId == CustomWebApplicationFactory.AdminUserId).AllAsync(s => !s.IsRevoked)).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(a => a.Action == SecurityAuditActions.RefreshTokenReuseDetected)).Should().BeFalse();

        // As duas abas seguem funcionando com os cookies que receberam.
        (await RefreshAsync(first.Cookie!.Value)).Response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(second.Cookie!.Value)).Response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TwoSimultaneousRefreshesWithTheSameCookie_BothSucceed()
    {
        var (_, loginCookie) = await AuthTestHelpers.LoginAsync(Factory.CreateClient(), Email, CustomWebApplicationFactory.DefaultPassword);

        var results = await Task.WhenAll(RefreshAsync(loginCookie.Value), RefreshAsync(loginCookie.Value));

        results.Should().OnlyContain(r => r.Response.StatusCode == HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.RefreshTokens.AnyAsync(t => t.ReasonRevoked == RefreshTokenRevocationReasons.ReuseDetected)).Should().BeFalse();
    }

    [Fact]
    public async Task ReusingARotatedCookie_OutsideGraceWindow_RevokesEverything()
    {
        var (_, loginCookie) = await AuthTestHelpers.LoginAsync(Factory.CreateClient(), Email, CustomWebApplicationFactory.DefaultPassword);

        var first = await RefreshAsync(loginCookie.Value);
        first.Response.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Clock.Advance(TimeSpan.FromSeconds(31));
        var reuse = await RefreshAsync(loginCookie.Value);

        reuse.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        reuse.Cookie.Should().NotBeNull();
        reuse.Cookie!.IsDeletion.Should().BeTrue();

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var tokens = await db.RefreshTokens.Where(t => t.UserId == CustomWebApplicationFactory.AdminUserId).ToListAsync();
        tokens.Should().OnlyContain(t => t.RevokedAt != null);
        tokens.Should().Contain(t => t.ReasonRevoked == RefreshTokenRevocationReasons.ReuseDetected);
        (await db.UserSessions.Where(s => s.UserId == CustomWebApplicationFactory.AdminUserId).AllAsync(s => s.IsRevoked)).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(a => a.Action == SecurityAuditActions.RefreshTokenReuseDetected
                                          && a.UserId == CustomWebApplicationFactory.AdminUserId)).Should().BeTrue();

        // O sucessor que a outra aba tinha também deixou de valer.
        (await RefreshAsync(first.Cookie!.Value)).Response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReusingATokenRevokedByLogout_WithinGraceWindow_IsStillTreatedAsReuse()
    {
        var client = Factory.CreateClient();
        var (login, loginCookie) = await AuthTestHelpers.LoginAsync(client, Email, CustomWebApplicationFactory.DefaultPassword);

        using var loggedIn = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, loginCookie.Value, login.AccessToken);
        (await loggedIn.PostAsync("/api/v1/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var reuse = await RefreshAsync(loginCookie.Value);
        reuse.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "só tokens rotacionados têm tolerância");
    }

    private async Task<(HttpResponseMessage Response, RefreshCookie? Cookie)> RefreshAsync(string refreshToken)
    {
        using var client = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, refreshToken);
        var response = await client.PostAsync("/api/v1/auth/refresh", null);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<Result<RefreshHttpResponse>>();
            body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        }
        return (response, RefreshCookie.From(response));
    }
}
