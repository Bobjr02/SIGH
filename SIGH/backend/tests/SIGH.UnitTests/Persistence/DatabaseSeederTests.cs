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
        (await context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id)).Should().Be(PermissionCodes.All.Count);

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
            (await context.RolePermissions.CountAsync()).Should().Be(PermissionCodes.All.Count);
            (await context.Roles.CountAsync()).Should().Be(1);
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
}
