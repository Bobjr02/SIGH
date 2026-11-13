using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Api.Contracts.Auth;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Users.Access;
using SIGH.Domain.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Parte D: a perda de acesso vale na requisição seguinte ao TTL do cache do SecurityStamp (30 s),
/// e não quando o JWT expira (60 min).
/// </summary>
public class AccessLossTests : IntegrationTestBase
{
    private static readonly TimeSpan PastTheCacheTtl = TimeSpan.FromSeconds(31);

    public AccessLossTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeactivatedUser_GetsA401OnTheNextRequestAfterTheCacheTtl_AndCannotRefresh()
    {
        var manager = await AccessTestHelpers.CreateUserAsync(Factory, "gestor131@sigh.test", SystemRoles.Manager, "USGA131");
        var client = Factory.CreateClient();
        var (login, cookie) = await AuthTestHelpers.LoginAsync(client, manager.Email, CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);
        (await client.GetAsync("/api/v1/employees")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        (await admin.PatchAsJsonAsync($"/api/v1/users/{manager.Id}/status", new { newStatus = UserStatus.Inactive, reason = "Desligado" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Clock.Advance(PastTheCacheTtl);
        var response = await client.GetAsync("/api/v1/employees");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("SESSION_REVOKED");

        // Refresh negado (usuário inativo) e refresh tokens revogados.
        using var refreshClient = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, cookie.Value);
        (await refreshClient.PostAsync("/api/v1/auth/refresh", null)).IsSuccessStatusCode.Should().BeFalse();
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.RefreshTokens.AnyAsync(t => t.UserId == manager.Id && t.RevokedAt == null)).Should().BeFalse();
    }

    [Fact]
    public async Task RemovingARole_RevokesTheSessionAfterTheCacheTtl_AndTheNextLoginHasNoSuchPermissions()
    {
        var user = await AccessTestHelpers.CreateUserAsync(Factory, "duplo@sigh.test",
            [new RoleGrant(SystemRoles.Legal), new RoleGrant(SystemRoles.HrApprover)]);
        using var client = await AccessTestHelpers.LoginAsync(Factory, user.Email);
        (await client.GetAsync("/api/v1/employees")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var access = (await admin.GetFromJsonAsync<Result<UserAccessDto>>($"/api/v1/users/{user.Id}/access"))!.Data!;
        var approverLink = access.Roles.Single(r => r.RoleName == SystemRoles.HrApprover);
        (await admin.DeleteAsync($"/api/v1/users/{user.Id}/roles/{approverLink.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Clock.Advance(PastTheCacheTtl);
        (await client.GetAsync("/api/v1/employees")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var again = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(again, user.Email, CustomWebApplicationFactory.DefaultPassword);
        login.User.Permissions.Should().NotContain(PermissionCodes.DisciplinaryRecords.Approve);
        login.User.Permissions.Should().Contain(PermissionCodes.Audit.View);
    }

    [Fact]
    public async Task RoleWithValidUntilInThePast_DoesNotCountWhenTheTokenIsIssued()
    {
        var now = Factory.Clock.UtcNow;
        var user = await AccessTestHelpers.CreateUserAsync(Factory, "substituto@sigh.test",
        [
            new RoleGrant(SystemRoles.Legal),
            new RoleGrant(SystemRoles.HrApprover, ValidFrom: now.AddDays(-20), ValidUntil: now.AddDays(-1)),
            new RoleGrant(SystemRoles.HrRegistrar, ValidFrom: now.AddDays(5)),
        ]);

        using var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, user.Email, CustomWebApplicationFactory.DefaultPassword);

        login.User.Roles.Should().Equal(SystemRoles.Legal);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        var permissions = token.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        permissions.Should().NotContain(PermissionCodes.DisciplinaryRecords.Approve, "o perfil venceu");
        permissions.Should().NotContain(PermissionCodes.DisciplinaryRecords.Register, "o perfil ainda não começou");
        token.Claims.Single(c => c.Type == SighClaimTypes.AccessChangesAt).Value
            .Should().Be(now.AddDays(5).ToUnixTimeSeconds().ToString(), "o próximo evento é o início do perfil agendado");
    }

    [Fact]
    public async Task RoleExpiringInTheMiddleOfTheToken_Gives401AtThatInstant_AndTheRefreshRecalculatesThePermissions()
    {
        var now = Factory.Clock.UtcNow;
        var user = await AccessTestHelpers.CreateUserAsync(Factory, "ferias@sigh.test",
            [new RoleGrant(SystemRoles.Legal), new RoleGrant(SystemRoles.HrApprover, ValidUntil: now.AddMinutes(10))]);

        var client = Factory.CreateClient();
        var (login, cookie) = await AuthTestHelpers.LoginAsync(client, user.Email, CustomWebApplicationFactory.DefaultPassword);
        login.User.Permissions.Should().Contain(PermissionCodes.DisciplinaryRecords.Approve);
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);

        Factory.Clock.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        var response = await client.GetAsync("/api/v1/employees");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ACCESS_CHANGED");

        using var refreshClient = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, cookie.Value);
        var refreshed = await refreshClient.PostAsync("/api/v1/auth/refresh", null);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await refreshed.Content.ReadFromJsonAsync<Result<RefreshHttpResponse>>())!.Data!;
        new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken).Claims
            .Where(c => c.Type == "permission").Select(c => c.Value)
            .Should().NotContain(PermissionCodes.DisciplinaryRecords.Approve);

        using var renewed = AuthTestHelpers.CreateClientWithoutCookieJar(Factory, accessToken: body.AccessToken);
        (await renewed.GetAsync("/api/v1/employees")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithAnOldStamp_IsRejected_ButLogoutStillWorks()
    {
        var user = await AccessTestHelpers.CreateUserAsync(Factory, "stamp@sigh.test", SystemRoles.Legal);
        using var client = await AccessTestHelpers.LoginAsync(Factory, user.Email);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var entity = await db.Users.SingleAsync(u => u.Id == user.Id);
            entity.RotateSecurityStamp();
            await db.SaveChangesAsync();
        }

        Factory.Clock.Advance(PastTheCacheTtl);
        (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/v1/auth/logout", null)).IsSuccessStatusCode.Should().BeTrue();
    }
}
