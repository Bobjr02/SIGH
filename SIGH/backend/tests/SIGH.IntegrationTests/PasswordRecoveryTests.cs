using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Authentication.ForgotPassword;
using SIGH.Application.Authentication.ResetPassword;
using SIGH.Application.Email;
using SIGH.Application.Security;
using SIGH.Application.Users.CreateUser;
using SIGH.Domain.Entities;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>Esqueci a senha / redefinição, lendo o e-mail gerado na outbox.</summary>
public class PasswordRecoveryTests : IntegrationTestBase
{
    private const string Email = "approver@sigh.com";
    private const string NewPassword = "Nova@Senha2026!";

    public PasswordRecoveryTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ForgotPassword_ReturnsTheSameResponseWhetherTheEmailExistsOrNot()
    {
        var client = Factory.CreateClient();

        var existing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(Email));
        var missing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest("nao.existe@sigh.com"));

        existing.StatusCode.Should().Be(HttpStatusCode.OK);
        missing.StatusCode.Should().Be(existing.StatusCode);
        (await missing.Content.ReadAsStringAsync()).Should().Be(await existing.Content.ReadAsStringAsync());

        var outbox = await ReadOutboxAsync();
        outbox.Should().ContainSingle().Which.To.Should().Be(Email);
    }

    [Fact]
    public async Task FullFlow_ResetWithTheEmailedLink_RevokesSessionsAndTheNewPasswordWorks()
    {
        var client = Factory.CreateClient();
        var (_, oldSessionCookie) = await AuthTestHelpers.LoginAsync(client, Email, CustomWebApplicationFactory.DefaultPassword);

        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(Email))).EnsureSuccessStatusCode();

        var message = (await ReadOutboxAsync()).Should().ContainSingle().Subject;
        message.Template.Should().Be(EmailTemplates.PasswordResetTemplate);
        message.Subject.Should().Contain("Redefinição de senha");
        var token = ExtractToken(message.TextBody);
        message.HtmlBody.Should().Contain(Uri.EscapeDataString(token));

        // Só o hash fica no banco.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            (await db.PasswordResetTokens.AnyAsync(t => t.TokenHash == token)).Should().BeFalse();
            (await db.PasswordResetTokens.CountAsync(t => t.UserId == CustomWebApplicationFactory.ApproverUserId && t.UsedAt == null)).Should().Be(1);
        }

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword));
        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());

        // Uso único.
        (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, "Outra@Senha2026!")))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // A sessão aberta antes do reset não renova mais.
        using var oldSession = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, oldSessionCookie.Value);
        (await oldSession.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Senha antiga não entra; a nova entra.
        (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = CustomWebApplicationFactory.DefaultPassword }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AuthTestHelpers.LoginAsync(Factory.CreateClient(), Email, NewPassword);

        await using var check = Factory.Services.CreateAsyncScope();
        var checkDb = check.ServiceProvider.GetRequiredService<SighDbContext>();
        (await checkDb.RefreshTokens.Where(t => t.UserId == CustomWebApplicationFactory.ApproverUserId && t.ReasonRevoked == "Senha redefinida").AnyAsync())
            .Should().BeTrue();
        (await checkDb.AuditLogs.AnyAsync(a => a.Action == SecurityAuditActions.PasswordReset && a.UserId == CustomWebApplicationFactory.ApproverUserId))
            .Should().BeTrue();
        var audit = string.Join("\n", (await checkDb.AuditLogs.ToListAsync()).Select(a => a.OldValues + a.NewValues));
        audit.Should().NotContain(token).And.NotContain(NewPassword);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var client = Factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(Email))).EnsureSuccessStatusCode();
        var token = ExtractToken((await ReadOutboxAsync()).Single().TextBody);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31)); // padrão: 30 min

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword));
        reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reset.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Detail.Should().Contain("inválido ou expirado");
    }

    [Fact]
    public async Task ANewRequest_InvalidatesThePreviousLink()
    {
        var client = Factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(Email))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(Email))).EnsureSuccessStatusCode();
        var messages = (await ReadOutboxAsync()).OrderBy(m => m.CreatedAt).ToList();
        var firstToken = ExtractToken(messages[0].TextBody);
        var secondToken = ExtractToken(messages[1].TextBody);

        (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(firstToken, NewPassword)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(secondToken, NewPassword)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_HasItsOwnRateLimit()
    {
        // Host separado: o limitador é em memória e por IP, não pode afetar os outros testes.
        await using var limited = Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimits:ForgotPassword:PermitLimit"] = "2" })));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest($"x{i}@sigh.com"))).StatusCode);

        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_KeepsItsRateLimit()
    {
        await using var limited = Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimits:Login:PermitLimit"] = "2" })));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "ninguem@sigh.com", password = "x" })).StatusCode);

        statuses.Should().Equal(HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CreatingAUser_SendsASetupLinkWithoutAnyPassword_AndTheLinkDefinesThePassword()
    {
        var admin = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(admin, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        Guid roleId;
        await using (var scope = Factory.Services.CreateAsyncScope())
            roleId = (await scope.ServiceProvider.GetRequiredService<SighDbContext>().Roles.SingleAsync(r => r.Name == DatabaseSeeder.AdministratorRoleName)).Id;

        var created = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Nova Pessoa", "nova.pessoa@sigh.com", "71428793860", InitialPassword: null, RoleIds: [roleId]));
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());

        var message = (await ReadOutboxAsync()).Should().ContainSingle(m => m.To == "nova.pessoa@sigh.com").Subject;
        message.Template.Should().Be(EmailTemplates.AccountCreatedTemplate);
        message.TextBody.Should().Contain("defina sua senha").And.Contain("&setup=1");
        message.TextBody.Should().NotMatchRegex("(?i)senha( inicial)?:\\s*\\S");

        var token = ExtractToken(message.TextBody);
        (await admin.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var (firstLogin, _) = await AuthTestHelpers.LoginAsync(Factory.CreateClient(), "nova.pessoa@sigh.com", NewPassword);
        firstLogin.MustChangePassword.Should().BeFalse("a pessoa já definiu a própria senha pelo link");
    }

    private async Task<List<EmailOutboxMessage>> ReadOutboxAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SighDbContext>().EmailOutbox.AsNoTracking().ToListAsync();
    }

    private static string ExtractToken(string body)
    {
        var match = Regex.Match(body, "/reset-password\\?token=([^&\\s]+)");
        match.Success.Should().BeTrue("o e-mail deve conter o link de redefinição");
        body.Should().Contain(CustomWebApplicationFactory.PublicBaseUrl + "/reset-password?token=", "o link usa App:PublicBaseUrl");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
