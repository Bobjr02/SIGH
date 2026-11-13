using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Interfaces;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.UnitTests.Persistence;

public class DatabaseSeederTests
{
    private const string AdminEmail = "admin@sigh.com.br";

    private static SighDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<SighDbContext>().UseInMemoryDatabase(dbName).Options);

    private static DatabaseSeeder CreateSeeder(SighDbContext context, string? adminPassword, string adminEmail = AdminEmail)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Seed:AdminEmail"] = adminEmail,
            ["Seed:DefaultCompanyName"] = "Empresa Teste",
            ["Seed:AdminPassword"] = adminPassword
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns<string>(p => $"hashed:{p}");

        return new DatabaseSeeder(context, hasher.Object, configuration, NullLogger<DatabaseSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesPermissionsRoleCompanyAndAdmin()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);

        await CreateSeeder(context, "SenhaForte@123").SeedAsync();

        context.Permissions.Select(p => p.Code).Should().BeEquivalentTo(PermissionCodes.All.Select(p => p.Code));

        var role = await context.Roles.SingleAsync(r => r.Name == DatabaseSeeder.AdministratorRoleName);
        (await PermissionsOf(context, SystemRoles.Administrator))
            .Should().BeEquivalentTo(SystemRoles.Find(SystemRoles.Administrator)!.Permissions);
        context.Roles.Select(r => r.Name).Should().BeEquivalentTo(SystemRoles.All.Select(r => r.Name));

        var company = await context.Companies.SingleAsync();
        company.Id.Should().Be(DatabaseSeeder.DefaultCompanyId);
        company.Name.Should().Be("Empresa Teste");

        var admin = await context.Users.Include(u => u.UserRoles).SingleAsync();
        admin.Email.Should().Be(AdminEmail);
        admin.PasswordHash.Should().Be("hashed:SenhaForte@123");
        admin.MustChangePassword.Should().BeTrue();
        admin.Status.Should().Be(UserStatus.PendingFirstAccess);
        admin.UserRoles.Should().ContainSingle(ur => ur.RoleId == role.Id);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateData()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var context = CreateContext(dbName))
            await CreateSeeder(context, "SenhaForte@123").SeedAsync();

        await using (var context = CreateContext(dbName))
        {
            await CreateSeeder(context, "SenhaForte@123").SeedAsync();

            (await context.Permissions.CountAsync()).Should().Be(PermissionCodes.All.Count);
            (await context.RolePermissions.CountAsync()).Should().Be(SystemRoles.All.Sum(r => r.Permissions.Count));
            (await context.Roles.CountAsync()).Should().Be(SystemRoles.All.Count);
            (await context.Companies.CountAsync()).Should().Be(1);
            (await context.Users.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task SeedAsync_WhenPermissionsAlreadyExistWithoutUsers_InsertsOnlyMissingOnes()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        context.Permissions.Add(new Permission { Code = PermissionCodes.Users.View, Name = "x", Description = "x" });
        await context.SaveChangesAsync();

        await CreateSeeder(context, adminPassword: null).SeedAsync();

        (await context.Permissions.CountAsync()).Should().Be(PermissionCodes.All.Count);
        (await context.Permissions.CountAsync(p => p.Code == PermissionCodes.Users.View)).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_WithoutAdminPassword_DoesNotCreateAdminButSeedsTheRest()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);

        await CreateSeeder(context, adminPassword: null).SeedAsync();

        (await context.Users.AnyAsync()).Should().BeFalse();
        (await context.Permissions.CountAsync()).Should().Be(PermissionCodes.All.Count);
        (await context.Companies.AnyAsync(c => c.Id == DatabaseSeeder.DefaultCompanyId)).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenAdminEmailChangesAfterAdminWasSeeded_DoesNotCreateSecondAdmin()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(dbName))
        {
            await CreateSeeder(context, adminPassword: "Senha@Inicial1").SeedAsync();
        }

        await using (var context = CreateContext(dbName))
        {
            var act = () => CreateSeeder(context, adminPassword: "Senha@Inicial1", adminEmail: "outro.admin@sigh.com.br").SeedAsync();

            await act.Should().NotThrowAsync();
            (await context.Users.Select(u => u.Email).ToListAsync()).Should().Equal(AdminEmail);
        }
    }

    [Fact]
    public async Task Administrator_NeverRegistersNorApprovesMeasures()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        await CreateSeeder(context, "SenhaForte@123").SeedAsync();

        var admin = await PermissionsOf(context, SystemRoles.Administrator);
        admin.Should().NotContain(PermissionCodes.DisciplinaryRecords.Register);
        admin.Should().NotContain(PermissionCodes.DisciplinaryRecords.Approve);
        admin.Should().Contain([PermissionCodes.Users.ManageRoles, PermissionCodes.DisciplinaryRecords.ViewAll]);
    }

    [Fact]
    public async Task NoSeededRole_ReceivesTheStepByStepProcessPermissions_ExceptEvidence()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        await CreateSeeder(context, "SenhaForte@123").SeedAsync();

        var granted = await context.RolePermissions.Select(rp => rp.Permission.Code).Distinct().ToListAsync();
        var legacy = typeof(PermissionCodes.DisciplinaryCases).GetFields()
            .Select(f => (string)f.GetValue(null)!)
            .Except([PermissionCodes.DisciplinaryCases.AddEvidence, PermissionCodes.DisciplinaryCases.RemoveEvidence]);
        granted.Should().NotIntersectWith(legacy);
    }

    [Fact]
    public async Task OnlyTheManager_HasNoFullScope()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        await CreateSeeder(context, "SenhaForte@123").SeedAsync();

        foreach (var role in SystemRoles.All)
        {
            var permissions = await PermissionsOf(context, role.Name);
            permissions.Should().BeEquivalentTo(role.Permissions, $"o perfil {role.Name} fica exatamente com a lista fixa");
            permissions.Contains(PermissionCodes.DisciplinaryRecords.ViewAll).Should().Be(role.Name != SystemRoles.Manager);
        }
    }

    [Fact]
    public async Task ExistingAdministratorWithAllPermissions_LosesTheExtraOnes_AndStaysAdministrator()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(dbName))
        {
            // Banco antigo: o seed dava TODAS as permissões ao Administrator.
            var role = new Role { Name = DatabaseSeeder.AdministratorRoleName, Description = "antigo" };
            context.Roles.Add(role);
            foreach (var definition in PermissionCodes.All)
            {
                var permission = new Permission { Code = definition.Code, Name = definition.Name, Description = definition.Description };
                context.Permissions.Add(permission);
                context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName))
        {
            await CreateSeeder(context, "SenhaForte@123").SeedAsync();

            (await PermissionsOf(context, SystemRoles.Administrator))
                .Should().BeEquivalentTo(SystemRoles.Find(SystemRoles.Administrator)!.Permissions);
            // Retirada lógica: o vínculo antigo fica na história.
            (await context.RolePermissions.IgnoreQueryFilters().CountAsync(rp => rp.IsDeleted)).Should().BeGreaterThan(0);
            var admin = await context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).SingleAsync();
            admin.UserRoles.Select(ur => ur.Role.Name).Should().Equal(SystemRoles.Administrator);
        }
    }

    private static Task<List<string>> PermissionsOf(SIGH.Persistence.Context.SighDbContext context, string roleName) =>
        context.RolePermissions.Where(rp => rp.Role.Name == roleName).Select(rp => rp.Permission.Code).ToListAsync();
}
