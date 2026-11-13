using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Security;
using SIGH.Application.Users.Access;
using SIGH.Domain.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>Parte B: regras de atribuição de perfis e gerências, com auditoria.</summary>
public class UserAccessAdministrationTests : IntegrationTestBase
{
    public UserAccessAdministrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> RoleIdAsync(HttpClient client, string name) =>
        (await client.GetFromJsonAsync<Result<List<RoleOptionDto>>>("/api/v1/roles"))!.Data!.Single(r => r.Name == name).Id;

    private async Task<Guid> UnitIdAsync(HttpClient client, string code) =>
        (await client.GetFromJsonAsync<Result<List<ManagementUnitDto>>>("/api/v1/management-units"))!.Data!.Single(m => m.Code == code).Id;

    private static async Task<string> DetailAsync(HttpResponseMessage response) =>
        System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("detail").GetString()!;

    [Fact]
    public async Task Roles_ListsTheFiveSystemRolesWithTheirFixedPermissions()
    {
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var roles = (await admin.GetFromJsonAsync<Result<List<RoleOptionDto>>>("/api/v1/roles"))!.Data!;

        roles.Select(r => r.Label).Should().Equal("Administrador", "RH – Registro", "RH – Aprovador", "Jurídico", "Gestor");
        roles.Single(r => r.Name == SystemRoles.Administrator).Permissions
            .Should().NotContain([PermissionCodes.DisciplinaryRecords.Register, PermissionCodes.DisciplinaryRecords.Approve]);
    }

    [Fact]
    public async Task NobodyChangesTheirOwnRolesOrUnitsOrStatus()
    {
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var self = CustomWebApplicationFactory.AdminUserId;
        var legal = await RoleIdAsync(admin, SystemRoles.Legal);

        (await admin.PostAsJsonAsync($"/api/v1/users/{self}/roles", new AssignRoleRequest(legal, null, null)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PostAsJsonAsync($"/api/v1/users/{self}/management-units", new AssignManagementUnitRequest(await UnitIdAsync(admin, "USGA131"), null, null)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PatchAsJsonAsync($"/api/v1/users/{self}/status", new { newStatus = UserStatus.Inactive, reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TheLastActiveAdministrator_CannotBeRemovedNorDeactivated()
    {
        // Ator sem perfil Administrador (perfil de teste), para que o alvo seja o último administrador ativo.
        await AccessTestHelpers.CreateUserAsync(Factory, "operador@sigh.test", CustomWebApplicationFactory.FullAccessTestRoleName);
        using var operador = await AccessTestHelpers.LoginAsync(Factory, "operador@sigh.test");

        // Dois administradores ativos (fixture): desativar um é permitido.
        (await operador.PatchAsJsonAsync($"/api/v1/users/{CustomWebApplicationFactory.ApproverUserId}/status", new { newStatus = UserStatus.Inactive, reason = "férias" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Agora admin@sigh.com é o último: nem retirar o perfil, nem desativar.
        var adminId = CustomWebApplicationFactory.AdminUserId;
        var access = (await operador.GetFromJsonAsync<Result<UserAccessDto>>($"/api/v1/users/{adminId}/access"))!.Data!;
        var link = access.Roles.Single(r => r.RoleName == SystemRoles.Administrator);

        var revoke = await operador.DeleteAsync($"/api/v1/users/{adminId}/roles/{link.Id}");
        revoke.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailAsync(revoke)).Should().Contain("último Administrador ativo");

        (await operador.PatchAsJsonAsync($"/api/v1/users/{adminId}/status", new { newStatus = UserStatus.Inactive, reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Limitar a validade do último administrador também é retirá-lo.
        var adminRole = await RoleIdAsync(operador, SystemRoles.Administrator);
        (await operador.PostAsJsonAsync($"/api/v1/users/{adminId}/roles", new AssignRoleRequest(adminRole, null, Factory.Clock.UtcNow.AddMinutes(-1))))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ManagerRole_RequiresAtLeastOneManagementUnit()
    {
        var target = await AccessTestHelpers.CreateUserAsync(Factory, "futuro.gestor@sigh.test", Array.Empty<RoleGrant>());
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var manager = await RoleIdAsync(admin, SystemRoles.Manager);

        var withoutUnit = await admin.PostAsJsonAsync($"/api/v1/users/{target.Id}/roles", new AssignRoleRequest(manager, null, null));
        withoutUnit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailAsync(withoutUnit)).Should().Contain("pelo menos uma gerência");

        // Um gestor pode responder por várias gerências (USGA137 e USGA141).
        await admin.PostAsJsonAsync($"/api/v1/users/{target.Id}/management-units", new AssignManagementUnitRequest(await UnitIdAsync(admin, "USGA137"), null, null));
        var second = await admin.PostAsJsonAsync($"/api/v1/users/{target.Id}/management-units", new AssignManagementUnitRequest(await UnitIdAsync(admin, "USGA141"), null, null));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsJsonAsync($"/api/v1/users/{target.Id}/roles", new AssignRoleRequest(manager, null, null))).StatusCode.Should().Be(HttpStatusCode.OK);

        var access = (await admin.GetFromJsonAsync<Result<UserAccessDto>>($"/api/v1/users/{target.Id}/access"))!.Data!;
        access.ManagementUnits.Select(m => m.Code).Should().Equal("USGA137", "USGA141");

        // Retirar uma ainda deixa outra; retirar a última é bloqueado.
        (await admin.DeleteAsync($"/api/v1/users/{target.Id}/management-units/{access.ManagementUnits[0].Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.DeleteAsync($"/api/v1/users/{target.Id}/management-units/{access.ManagementUnits[1].Id}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EveryAssignmentAndRevocation_IsAudited_WithActorTargetRoleAndValidity()
    {
        var target = await AccessTestHelpers.CreateUserAsync(Factory, "auditado@sigh.test", SystemRoles.Legal);
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var approver = await RoleIdAsync(admin, SystemRoles.HrApprover);
        var from = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(-3));
        var until = new DateTimeOffset(2026, 12, 21, 0, 0, 0, TimeSpan.FromHours(-3));

        var assigned = (await (await admin.PostAsJsonAsync($"/api/v1/users/{target.Id}/roles", new AssignRoleRequest(approver, from, until)))
            .Content.ReadFromJsonAsync<Result<UserAccessDto>>())!.Data!;
        var link = assigned.Roles.Single(r => r.RoleName == SystemRoles.HrApprover);
        link.State.Should().Be(ValidityState.Scheduled, "a validade começa no futuro");
        (await admin.DeleteAsync($"/api/v1/users/{target.Id}/roles/{link.Id}")).EnsureSuccessStatusCode();

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var events = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == SecurityAuditActions.RoleAssigned || a.Action == SecurityAuditActions.RoleRevoked)
            .OrderBy(a => a.CreatedAt).ToListAsync();

        events.Select(e => e.Action).Should().Equal(SecurityAuditActions.RoleAssigned, SecurityAuditActions.RoleRevoked);
        events.Should().AllSatisfy(e =>
        {
            e.UserId.Should().Be(CustomWebApplicationFactory.AdminUserId, "quem fez");
            e.NewValues.Should().Contain(target.Id.ToString(), "para quem").And.Contain(SystemRoles.HrApprover).And.Contain("2026-12-21");
        });
    }

    [Fact]
    public async Task ManagementUnits_AreListedAndRenamed_ByTheAdministrator()
    {
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var units = (await admin.GetFromJsonAsync<Result<List<ManagementUnitDto>>>("/api/v1/management-units"))!.Data!;
        units.Should().HaveCount(9);

        var id = units.Single(u => u.Code == "USGA131").Id;
        (await admin.PutAsJsonAsync($"/api/v1/management-units/{id}", new RenameManagementUnitRequest("Industrial (novo nome)")))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var legal = await AccessTestHelpers.CreateUserAsync(Factory, "juridico@sigh.test", SystemRoles.Legal);
        using var legalClient = await AccessTestHelpers.LoginAsync(Factory, legal.Email);
        (await legalClient.PutAsJsonAsync($"/api/v1/management-units/{id}", new RenameManagementUnitRequest("x")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
