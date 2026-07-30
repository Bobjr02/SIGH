using Microsoft.EntityFrameworkCore;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Seeds;

public static class DbInitializer
{
    public static async Task SeedAsync(SighDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return; // Seed já executado
        }

        // 1. Criar Permissões Iniciais
        var permissions = new List<Permission>
        {
            new Permission
            {
                Code = "Users.Create",
                Name = "Criar Usuários",
                Description = "Permissão para criar novos usuários no sistema"
            },
            new Permission
            {
                Code = "Users.Update",
                Name = "Atualizar Usuários",
                Description = "Permissão para atualizar dados e status de usuários"
            },
            new Permission
            {
                Code = "Users.Delete",
                Name = "Excluir Usuários",
                Description = "Permissão para inativar/remover usuários"
            },
            new Permission
            {
                Code = "Users.Read",
                Name = "Visualizar Usuários",
                Description = "Permissão para consultar a lista e detalhes de usuários"
            },
            new Permission
            {
                Code = "Roles.Manage",
                Name = "Gerenciar Perfis",
                Description = "Permissão para gerenciar os perfis do sistema (RBAC)"
            },
            new Permission
            {
                Code = "Permissions.Manage",
                Name = "Gerenciar Permissões",
                Description = "Permissão para gerenciar a matriz de permissões"
            },
            new Permission
            {
                Code = "Auth.Manage",
                Name = "Gerenciar Autenticação",
                Description = "Permissão para administração geral de autenticação e sessões"
            }
        };

        await context.Permissions.AddRangeAsync(permissions);

        // 2. Criar Perfil Administrador
        var adminRole = new Role
        {
            Name = "Administrator",
            Description = "Perfil com acesso irrestrito e total ao sistema SIGH"
        };

        await context.Roles.AddAsync(adminRole);

        // 3. Vincular todas as permissões ao Perfil Administrador (RolePermissions)
        foreach (var permission in permissions)
        {
            await context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // 4. Criar Usuário Administrador
        var adminUser = new User
        {
            FullName = "Administrador do Sistema",
            Email = "admin@sigh.com.br",
            Cpf = "00000000000",
            PasswordHash = "$2a$12$PlaceholderHashForInitialSeedAdminPassword2026!",
            Status = UserStatus.Active,
            MustChangePassword = false,
            FailedLoginAttempts = 0
        };

        await context.Users.AddAsync(adminUser);

        // 5. Vincular Usuário Administrador ao Perfil Administrator (UserRole)
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }
}
