using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIGH.Application.Interfaces;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Persistence.Context;

namespace SIGH.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SighDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SighDbContext>>();

            services.AddDbContext<SighDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            db.Database.EnsureCreated();

            SeedDatabase(db, passwordHasher);
        });
    }

    private static void SeedDatabase(SighDbContext db, IPasswordHasher passwordHasher)
    {
        if (db.Users.Any()) return;

        // Seeds
        var createPerm = new Permission(Guid.NewGuid())
        {
            Code = "Users.Create",
            Name = "Criar Usuário",
            Description = "Permite criar novos usuários"
        };

        var statusPerm = new Permission(Guid.NewGuid())
        {
            Code = "Users.ChangeStatus",
            Name = "Alterar Status do Usuário",
            Description = "Permite alterar status do usuário"
        };

        var unlockPerm = new Permission(Guid.NewGuid())
        {
            Code = "Users.Unlock",
            Name = "Desbloquear Usuário",
            Description = "Permite desbloquear usuário"
        };

        var viewPerm = new Permission(Guid.NewGuid())
        {
            Code = "Users.View",
            Name = "Visualizar Usuários",
            Description = "Permite visualizar usuários"
        };

        var discPerms = new[]
        {
            "Disciplinary.InfractionTypes.Create",
            "Disciplinary.InfractionTypes.View",
            "Disciplinary.InfractionTypes.Update",
            "Disciplinary.InfractionTypes.Activate",
            "Disciplinary.InfractionTypes.Deactivate",
            "Disciplinary.Cases.Create",
            "Disciplinary.Cases.View",
            "Disciplinary.Cases.Open",
            "Disciplinary.Cases.StartInvestigation",
            "Disciplinary.Cases.SubmitForDecision",
            "Disciplinary.Cases.ApproveDecision",
            "Disciplinary.Cases.RejectDecision",
            "Disciplinary.Cases.AddOccurrence",
            "Disciplinary.Cases.AddEmployee",
            "Disciplinary.Cases.AddEvidence",
            "Disciplinary.Cases.RecordDecision",
            "Disciplinary.Cases.ApplyMeasure",
            "Disciplinary.Cases.Cancel",
            "Disciplinary.Cases.Conclude"
        }.Select(code => new Permission(Guid.NewGuid())
        {
            Code = code,
            Name = code,
            Description = code
        }).ToList();

        db.Permissions.AddRange(createPerm, statusPerm, unlockPerm, viewPerm);
        db.Permissions.AddRange(discPerms);

        var adminRole = new Role(Guid.NewGuid())
        {
            Name = "Admin",
            Description = "Administrador do Sistema"
        };

        db.Roles.Add(adminRole);

        db.RolePermissions.AddRange(
            new RolePermission(Guid.NewGuid()) { RoleId = adminRole.Id, PermissionId = createPerm.Id },
            new RolePermission(Guid.NewGuid()) { RoleId = adminRole.Id, PermissionId = statusPerm.Id },
            new RolePermission(Guid.NewGuid()) { RoleId = adminRole.Id, PermissionId = unlockPerm.Id },
            new RolePermission(Guid.NewGuid()) { RoleId = adminRole.Id, PermissionId = viewPerm.Id }
        );

        foreach (var p in discPerms)
        {
            db.RolePermissions.Add(new RolePermission(Guid.NewGuid()) { RoleId = adminRole.Id, PermissionId = p.Id });
        }

        var passwordHash = passwordHasher.HashPassword("Admin@123456");

        var adminUser = new User(Guid.NewGuid())
        {
            FullName = "Administrador do Sistema",
            Email = "admin@sigh.com",
            Cpf = "12345678901",
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            MustChangePassword = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(adminUser);

        db.UserRoles.Add(new UserRole(Guid.NewGuid())
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        var lockedUser = new User(Guid.NewGuid())
        {
            FullName = "Usuário Bloqueado",
            Email = "locked@sigh.com",
            Cpf = "98765432100",
            PasswordHash = passwordHash,
            Status = UserStatus.Locked,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(1),
            MustChangePassword = false,
            FailedLoginAttempts = 5,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var unprivilegedUser = new User(Guid.NewGuid())
        {
            FullName = "Usuário Sem Permissao",
            Email = "unprivileged@sigh.com",
            Cpf = "11122233344",
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            MustChangePassword = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Users.AddRange(lockedUser, unprivilegedUser);

        db.SaveChanges();
    }
}
