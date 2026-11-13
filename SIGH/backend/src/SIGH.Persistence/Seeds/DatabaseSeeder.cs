using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Interfaces;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Seeds;

/// <summary>
/// Seed idempotente dos dados mínimos para o sistema funcionar num banco novo:
/// catálogo de permissões, perfis de sistema (<see cref="SystemRoles"/>), empresa padrão e usuário administrador.
/// Pode ser executado a cada inicialização: só insere o que estiver faltando, e as permissões de cada
/// perfil de sistema são sincronizadas com a lista fixa (o que sobrar é retirado logicamente).
/// </summary>
public sealed class DatabaseSeeder
{
    public const string AdministratorRoleName = SystemRoles.Administrator;

    /// <summary>
    /// Empresa padrão da qual o AuthorizedCompanyProvider depende.
    /// </summary>
    public static readonly Guid DefaultCompanyId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // Instalação de empresa única: todos os funcionários pertencem à empresa padrão.
    private const string FallbackCompanyName = "Usina Serra Grande S.A.";
    // Nome antigo do seed: renomeado automaticamente para o nome configurado.
    private const string LegacyCompanyName = "Empresa Padrão";
    private const string AdminFullName = "Administrador do Sistema";
    private const string AdminCpf = "00000000000";

    private readonly SighDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        SighDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await EnsurePermissionsAsync(cancellationToken);
        var roles = await EnsureSystemRolesAsync(permissions, cancellationToken);
        var adminRole = roles[SystemRoles.Administrator];
        await EnsureDefaultCompanyAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await EnsureManagementUnitsAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await EnsureAdminUserAsync(adminRole, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Permission>> EnsurePermissionsAsync(CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: uma permissão excluída logicamente é restaurada em vez de duplicada.
        var existing = await _context.Permissions
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.Code, StringComparer.Ordinal, cancellationToken);

        var result = new List<Permission>();
        var inserted = 0;

        foreach (var definition in PermissionCodes.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                if (permission.IsDeleted)
                {
                    permission.IsDeleted = false;
                    permission.DeletedAt = null;
                    permission.DeletedBy = null;
                }
            }
            else
            {
                permission = new Permission
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description
                };
                await _context.Permissions.AddAsync(permission, cancellationToken);
                inserted++;
            }

            result.Add(permission);
        }

        if (inserted > 0)
            _logger.LogInformation("Seed: {Count} permissões inseridas.", inserted);

        return result;
    }

    private async Task<Dictionary<string, Role>> EnsureSystemRolesAsync(IReadOnlyList<Permission> permissions, CancellationToken cancellationToken)
    {
        var permissionByCode = permissions.ToDictionary(p => p.Code, StringComparer.Ordinal);
        var result = new Dictionary<string, Role>(StringComparer.Ordinal);

        foreach (var definition in SystemRoles.All)
        {
            var role = await _context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Name == definition.Name, cancellationToken);

            if (role is null)
            {
                role = new Role { Name = definition.Name, Description = definition.Description };
                await _context.Roles.AddAsync(role, cancellationToken);
                _logger.LogInformation("Seed: perfil {Role} criado.", definition.Name);
            }
            else
            {
                if (role.IsDeleted)
                {
                    role.IsDeleted = false;
                    role.DeletedAt = null;
                    role.DeletedBy = null;
                }
                if (role.Description != definition.Description)
                    role.Description = definition.Description;
            }

            await SyncRolePermissionsAsync(role, definition, permissionByCode, cancellationToken);
            result[definition.Name] = role;
        }

        return result;
    }

    /// <summary>
    /// Deixa o perfil com EXATAMENTE as permissões da definição: concede as que faltam e retira
    /// (exclusão lógica, auditada) as que sobram — é assim que o Administrador de bancos antigos
    /// perde as permissões de medida e do processo em etapas.
    /// </summary>
    private async Task SyncRolePermissionsAsync(
        Role role,
        SystemRoleDefinition definition,
        IReadOnlyDictionary<string, Permission> permissionByCode,
        CancellationToken cancellationToken)
    {
        var wanted = definition.Permissions.Select(code => permissionByCode[code].Id).ToHashSet();

        var links = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        var granted = 0;
        var revoked = 0;
        foreach (var link in links)
        {
            if (wanted.Contains(link.PermissionId))
            {
                if (link.IsDeleted)
                {
                    link.IsDeleted = false;
                    link.DeletedAt = null;
                    link.DeletedBy = null;
                    granted++;
                }
            }
            else if (!link.IsDeleted)
            {
                link.IsDeleted = true;
                link.DeletedAt = DateTimeOffset.UtcNow;
                revoked++;
            }
        }

        var existing = links.Select(l => l.PermissionId).ToHashSet();
        foreach (var permissionId in wanted.Where(id => !existing.Contains(id)))
        {
            await _context.RolePermissions.AddAsync(new RolePermission { RoleId = role.Id, PermissionId = permissionId }, cancellationToken);
            granted++;
        }

        if (granted > 0 || revoked > 0)
            _logger.LogInformation("Seed: perfil {Role}: {Granted} permissões concedidas, {Revoked} retiradas.", definition.Name, granted, revoked);
    }

    private async Task EnsureDefaultCompanyAsync(CancellationToken cancellationToken)
    {
        var name = _configuration["Seed:DefaultCompanyName"];
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning(
                "Seed: 'Seed:DefaultCompanyName' não configurado. A empresa padrão usa o nome '{Name}'.",
                FallbackCompanyName);
            name = FallbackCompanyName;
        }

        var existing = await _context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == DefaultCompanyId, cancellationToken);

        if (existing is not null)
        {
            // Só troca o nome legado do seed; um nome alterado por alguém é preservado.
            if (existing.Name == LegacyCompanyName && name != LegacyCompanyName)
            {
                existing.UpdateName(name);
                _logger.LogInformation("Seed: empresa padrão renomeada para {Name}.", name);
            }
            return;
        }

        var company = new Company(DefaultCompanyId);
        company.UpdateName(name);
        await _context.Companies.AddAsync(company, cancellationToken);
        _logger.LogInformation("Seed: empresa padrão {CompanyId} criada.", DefaultCompanyId);
    }

    /// <summary>
    /// Gerências (Chave Organização do SAP) de Seed:ManagementUnits (código e nome). Só cria as que
    /// faltam: nomes já existentes nunca são sobrescritos, para continuarem editáveis.
    /// </summary>
    private async Task EnsureManagementUnitsAsync(CancellationToken cancellationToken)
    {
        var configured = _configuration.GetSection("Seed:ManagementUnits").GetChildren()
            .Select(s => (Code: s["Code"]?.Trim(), Name: s["Name"]?.Trim()))
            .Where(x => !string.IsNullOrEmpty(x.Code) && !string.IsNullOrEmpty(x.Name))
            .ToList();
        if (configured.Count == 0)
            return;

        var existingCodes = await _context.ManagementUnits
            .IgnoreQueryFilters()
            .Where(m => m.CompanyId == DefaultCompanyId && m.Code != null)
            .Select(m => m.Code!)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var (code, name) in configured)
        {
            if (existingCodes.Contains(code!, StringComparer.OrdinalIgnoreCase))
                continue;
            await _context.ManagementUnits.AddAsync(ManagementUnit.Create(DefaultCompanyId, name!, code), cancellationToken);
            created++;
        }

        if (created > 0)
            _logger.LogInformation("Seed: {Count} gerências criadas.", created);
    }

    private async Task EnsureAdminUserAsync(Role adminRole, CancellationToken cancellationToken)
    {
        var email = _configuration["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Seed: 'Seed:AdminEmail' não configurado. O usuário administrador NÃO foi criado.");
            return;
        }

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
            return;

        // O admin do seed usa um CPF fixo (índice único). Se ele já foi criado com outro e-mail
        // (Seed:AdminEmail mudou), não tenta criar um segundo: o startup quebraria com violação de índice.
        var existingSeedAdminEmail = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Cpf == AdminCpf)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSeedAdminEmail is not null)
        {
            _logger.LogWarning(
                "Seed: o administrador inicial já existe com o e-mail {ExistingEmail}; {Email} (Seed:AdminEmail) NÃO foi criado.",
                existingSeedAdminEmail, email);
            return;
        }

        var password = _configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Seed: 'Seed:AdminPassword' não configurada. O usuário administrador {Email} NÃO foi criado. " +
                "Configure-a via user-secrets ou variável de ambiente Seed__AdminPassword e reinicie a aplicação.",
                email);
            return;
        }

        var passwordHash = _passwordHasher.HashPassword(password);

        var admin = new User
        {
            FullName = AdminFullName,
            Email = email,
            Cpf = AdminCpf,
            PasswordHash = passwordHash,
            Status = UserStatus.PendingFirstAccess,
            MustChangePassword = true,
            FailedLoginAttempts = 0
        };

        admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        admin.PasswordHistories.Add(new PasswordHistory { UserId = admin.Id, PasswordHash = passwordHash });

        await _context.Users.AddAsync(admin, cancellationToken);
        _logger.LogInformation("Seed: usuário administrador {Email} criado (troca de senha obrigatória).", email);
    }
}
