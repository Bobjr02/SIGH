using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Interfaces;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Persistence.Context;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>Usuário de teste criado com os perfis de sistema reais (e gerências).</summary>
public sealed record TestUser(Guid Id, string Email);

public sealed record RoleGrant(string Role, DateTimeOffset? ValidFrom = null, DateTimeOffset? ValidUntil = null);

public static class AccessTestHelpers
{
    /// <summary>Cria um usuário ativo, com senha <see cref="CustomWebApplicationFactory.DefaultPassword"/>.</summary>
    public static async Task<TestUser> CreateUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        IEnumerable<RoleGrant> roles,
        params string[] managementUnitCodes)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User(Guid.NewGuid())
        {
            FullName = email.Split('@')[0],
            Email = email,
            Cpf = Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString(),
            PasswordHash = hasher.HashPassword(CustomWebApplicationFactory.DefaultPassword),
            Status = UserStatus.Active,
            MustChangePassword = false
        };
        db.Users.Add(user);

        foreach (var grant in roles)
        {
            var role = await db.Roles.SingleAsync(r => r.Name == grant.Role);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, ValidFrom = grant.ValidFrom, ValidUntil = grant.ValidUntil });
        }

        foreach (var code in managementUnitCodes)
        {
            var unit = await db.ManagementUnits.SingleAsync(m => m.Code == code);
            db.UserManagementUnits.Add(new UserManagementUnit { UserId = user.Id, ManagementUnitId = unit.Id });
        }

        await db.SaveChangesAsync();
        return new TestUser(user.Id, email);
    }

    public static Task<TestUser> CreateUserAsync(CustomWebApplicationFactory factory, string email, string role, params string[] managementUnitCodes) =>
        CreateUserAsync(factory, email, [new RoleGrant(role)], managementUnitCodes);

    /// <summary>Cliente autenticado (login real, token no header).</summary>
    public static async Task<HttpClient> LoginAsync(CustomWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, email, CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    /// <summary>Perfil só de teste com as permissões informadas (idempotente).</summary>
    public static async Task<string> CreateRoleAsync(CustomWebApplicationFactory factory, string name, params string[] permissions)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        if (!await db.Roles.AnyAsync(r => r.Name == name))
        {
            var role = new Role(Guid.NewGuid()) { Name = name, Description = "Somente testes" };
            db.Roles.Add(role);
            foreach (var permission in await db.Permissions.Where(p => permissions.Contains(p.Code)).ToListAsync())
                db.RolePermissions.Add(new RolePermission(Guid.NewGuid()) { RoleId = role.Id, PermissionId = permission.Id });
            await db.SaveChangesAsync();
        }
        return name;
    }

    /// <summary>Fixture admin@sigh.com (Administrador + perfil de teste com tudo).</summary>
    public static Task<HttpClient> FixtureAdminAsync(CustomWebApplicationFactory factory) => LoginAsync(factory, "admin@sigh.com");
}
