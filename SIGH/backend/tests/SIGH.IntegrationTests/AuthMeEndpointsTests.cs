using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Authentication.CurrentUser;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests;

public class AuthMeEndpointsTests : IntegrationTestBase
{
    public AuthMeEndpointsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Me_ReturnsProfileRolesAndEffectivePermissionsWithoutCpf()
    {
        using var client = await CreateAuthenticatedClientAsync("admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using (var document = JsonDocument.Parse(json))
        {
            document.RootElement.GetProperty("data").TryGetProperty("cpf", out _).Should().BeFalse();
        }

        var me = JsonSerializer.Deserialize<Result<CurrentUserResponse>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Data!;
        me.Id.Should().Be(CustomWebApplicationFactory.AdminUserId);
        me.FullName.Should().Be("Administrador do Sistema");
        me.Email.Should().Be("admin@sigh.com");
        me.Status.Should().Be("Active");
        me.MustChangePassword.Should().BeFalse();
        me.Roles.Should().Equal(DatabaseSeeder.AdministratorRoleName);
        me.Permissions.Should().BeEquivalentTo(PermissionCodes.All.Select(p => p.Code));
    }

    [Fact]
    public async Task Me_ReadsPermissionsFromDatabaseNotFromToken()
    {
        using var client = await CreateAuthenticatedClientAsync("admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);

        // Depois do login, o perfil é retirado do usuário. O token ainda carrega as permissões antigas.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            await db.UserRoles.Where(ur => ur.UserId == CustomWebApplicationFactory.AdminUserId).ExecuteDeleteAsync();
        }

        var me = (await client.GetFromJsonAsync<Result<CurrentUserResponse>>("/api/v1/auth/me"))!.Data!;

        me.Roles.Should().BeEmpty();
        me.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Me_IsAllowedWhilePasswordChangeIsRequired()
    {
        using var client = await CreateAuthenticatedClientAsync(CustomWebApplicationFactory.SeedAdminEmail, CustomWebApplicationFactory.SeedAdminPassword);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = (await response.Content.ReadFromJsonAsync<Result<CurrentUserResponse>>())!.Data!;
        me.MustChangePassword.Should().BeTrue();
        me.Status.Should().Be("PendingFirstAccess");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = Factory.CreateClient();

        (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
}
