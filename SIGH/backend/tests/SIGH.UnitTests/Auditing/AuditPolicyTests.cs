using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Persistence.Auditing;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests.Auditing;

public class AuditPolicyTests
{
    private static SighDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SighDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void ExclusionList_ContainsTheKnownSecrets()
    {
        AuditPolicy.SensitiveProperties.Should().Contain(new[] { "PasswordHash", "TokenHash", "ReplacedByTokenHash", "SecretKey", "Password" });
    }

    [Fact]
    public void EveryMappedPropertyThatLooksLikeASecret_IsInTheExclusionList()
    {
        using var context = CreateContext();
        var suspicious = new Regex("password|token|secret|hash", RegexOptions.IgnoreCase);

        var missing = context.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties().Select(p => (Entity: t.ClrType.Name, Property: p.Name)))
            .Where(x => suspicious.IsMatch(x.Property))
            .Where(x => !AuditPolicy.SensitiveProperties.Contains(x.Property) && !AuditPolicy.NonSecretProperties.Contains(x.Property))
            .Select(x => $"{x.Entity}.{x.Property}")
            .ToList();

        missing.Should().BeEmpty("propriedades com cara de segredo precisam estar em AuditPolicy.SensitiveProperties (ou, se não forem segredo, em NonSecretProperties)");
    }

    [Fact]
    public async Task CreatingAUser_RecordsCreatedWithoutPasswordHashOrCpf()
    {
        await using var context = CreateContext();
        var user = new User { FullName = "Ana Auditada", Email = "ana@sigh.test", Cpf = "12345678901", PasswordHash = "$2a$11$super-secret-hash", Status = UserStatus.Active };
        context.Users.Add(user);

        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync(a => a.Entity == nameof(User));
        log.Action.Should().Be(AuditActions.Created);
        log.EntityId.Should().Be(user.Id.ToString());
        log.NewValues.Should().Contain("Ana Auditada");
        log.NewValues.Should().NotContain("super-secret-hash").And.NotContain("12345678901");
        log.NewValues.Should().Contain($"\"PasswordHash\":\"{AuditPolicy.Redacted}\"");
    }

    [Fact]
    public async Task UpdatingAUser_RecordsOnlyChangedFieldsWithOldAndNewValues()
    {
        await using var context = CreateContext();
        var user = new User { FullName = "Nome Antigo", Email = "x@sigh.test", Cpf = "1", PasswordHash = "old-hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FullName = "Nome Novo";
        user.PasswordHash = "new-hash";
        user.LastLoginAt = DateTimeOffset.UtcNow; // volátil: não entra
        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync(a => a.Action == AuditActions.Updated);
        log.OldValues.Should().Be($"{{\"FullName\":\"Nome Antigo\",\"PasswordHash\":\"{AuditPolicy.Redacted}\"}}");
        log.NewValues.Should().Be($"{{\"FullName\":\"Nome Novo\",\"PasswordHash\":\"{AuditPolicy.Redacted}\"}}");
    }

    [Fact]
    public async Task SoftDeletingAnEntity_RecordsSoftDeleted()
    {
        await using var context = CreateContext();
        var role = new Role { Name = "Perfil", Description = "d" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        context.Roles.Remove(role); // o DbContext converte em exclusão lógica
        await context.SaveChangesAsync();

        (await context.AuditLogs.CountAsync(a => a.Action == AuditActions.SoftDeleted && a.EntityId == role.Id.ToString())).Should().Be(1);
    }

    [Fact]
    public async Task NonAuditedEntities_AreNotRecorded()
    {
        await using var context = CreateContext();
        context.RefreshTokens.Add(new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "rt-hash", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        await context.SaveChangesAsync();

        (await context.AuditLogs.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ChangingRoleAssignments_RecordsASecurityEvent()
    {
        await using var context = CreateContext();
        context.UserRoles.Add(new UserRole { UserId = Guid.NewGuid(), RoleId = Guid.NewGuid() });

        await context.SaveChangesAsync();

        (await context.AuditLogs.AnyAsync(a => a.Action == SIGH.Application.Security.SecurityAuditActions.RolesOrPermissionsChanged)).Should().BeTrue();
    }
}
