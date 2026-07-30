using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SighDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

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
        var createPerm = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Users.Create",
            Name = "Criar Usuário",
            Description = "Permite criar novos usuários",
            Category = "Users"
        };

        var statusPerm = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Users.ChangeStatus",
            Name = "Alterar Status do Usuário",
            Description = "Permite alterar status do usuário",
            Category = "Users"
        };

        var unlockPerm = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Users.Unlock",
            Name = "Desbloquear Usuário",
            Description = "Permite desbloquear usuário",
            Category = "Users"
        };

        var viewPerm = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Users.View",
            Name = "Visualizar Usuários",
            Description = "Permite visualizar usuários",
            Category = "Users"
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
        }.Select(code => new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Description = code,
            Category = "Disciplinary"
        }).ToList();

        db.Permissions.AddRange(createPerm, statusPerm, unlockPerm, viewPerm);
        db.Permissions.AddRange(discPerms);

        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Description = "Administrador do Sistema"
        };

        db.Roles.Add(adminRole);

        db.RolePermissions.AddRange(
            new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = createPerm.Id },
            new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = statusPerm.Id },
            new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = unlockPerm.Id },
            new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = viewPerm.Id }
        );

        foreach (var p in discPerms)
        {
            db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = p.Id });
        }

        var passwordHash = passwordHasher.HashPassword("Admin@123456");

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador do Sistema",
            Email = "admin@sigh.com",
            NormalizedEmail = "ADMIN@SIGH.COM",
            Cpf = "12345678901",
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            MustChangePassword = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(adminUser);

        db.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        var lockedUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Usuário Bloqueado",
            Email = "locked@sigh.com",
            NormalizedEmail = "LOCKED@SIGH.COM",
            Cpf = "98765432100",
            PasswordHash = passwordHash,
            Status = UserStatus.Locked,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(1),
            MustChangePassword = false,
            FailedLoginAttempts = 5,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var unprivilegedUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Usuário Sem Permissao",
            Email = "unprivileged@sigh.com",
            NormalizedEmail = "UNPRIVILEGED@SIGH.COM",
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
