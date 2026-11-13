using System.Security;
using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Security;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Users.Access;

/// <summary>Estado da vigência de um vínculo no momento da consulta.</summary>
public enum ValidityState
{
    Active = 1,
    /// <summary>Ainda vai começar (ValidFrom no futuro).</summary>
    Scheduled = 2,
    /// <summary>Venceu (ValidUntil no passado).</summary>
    Expired = 3
}

public sealed record RoleOptionDto(Guid Id, string Name, string Label, string Description, IReadOnlyList<string> Permissions);

public sealed record UserRoleDto(
    Guid Id, Guid RoleId, string RoleName, string RoleLabel,
    DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil, ValidityState State);

public sealed record UserManagementUnitDto(
    Guid Id, Guid ManagementUnitId, string? Code, string Name,
    DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil, ValidityState State);

public sealed record UserAccessDto(Guid UserId, IReadOnlyList<UserRoleDto> Roles, IReadOnlyList<UserManagementUnitDto> ManagementUnits);

public sealed record AssignRoleRequest(Guid RoleId, DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil);

public sealed record AssignManagementUnitRequest(Guid ManagementUnitId, DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil);

public sealed record ManagementUnitDto(Guid Id, string? Code, string Name, bool IsActive);

public sealed record RenameManagementUnitRequest(string Name);

public interface IUserAccessAdministrationService
{
    Task<IReadOnlyList<RoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<UserAccessDto> GetUserAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAccessDto> AssignRoleAsync(Guid actorUserId, Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task<UserAccessDto> RevokeRoleAsync(Guid actorUserId, Guid userId, Guid userRoleId, CancellationToken cancellationToken = default);
    Task<UserAccessDto> AssignManagementUnitAsync(Guid actorUserId, Guid userId, AssignManagementUnitRequest request, CancellationToken cancellationToken = default);
    Task<UserAccessDto> RevokeManagementUnitAsync(Guid actorUserId, Guid userId, Guid userManagementUnitId, CancellationToken cancellationToken = default);
}

public interface IManagementUnitAdministrationService
{
    Task<IReadOnlyList<ManagementUnitDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ManagementUnitDto> RenameAsync(Guid id, RenameManagementUnitRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Regras de atribuição que valem para qualquer caminho (perfis, gerências e status do usuário):
/// ninguém altera o próprio acesso; nunca fica o sistema sem um Administrador ativo.
/// </summary>
public static class UserAccessRules
{
    public const string SelfChangeMessage = "Ninguém pode alterar os próprios perfis, gerências ou status. Peça a outro administrador.";
    public const string LastAdministratorMessage = "Não é possível retirar ou desativar o último Administrador ativo.";
    public const string ManagerWithoutUnitMessage = "O perfil Gestor exige pelo menos uma gerência vinculada ao usuário.";

    public static void EnsureNotSelf(Guid actorUserId, Guid targetUserId)
    {
        if (actorUserId == targetUserId)
            throw new SecurityException(SelfChangeMessage);
    }

    /// <summary>Administradores ativos: status Ativo e perfil Administrator vigente agora.</summary>
    public static IQueryable<Guid> ActiveAdministratorIds(IApplicationDbContext db, DateTimeOffset now) =>
        db.UserRoles
            .Where(ur => ur.Role.Name == SystemRoles.Administrator && !ur.Role.IsDeleted
                         && (ur.ValidFrom == null || ur.ValidFrom <= now)
                         && (ur.ValidUntil == null || ur.ValidUntil > now)
                         && ur.User.Status == UserStatus.Active && !ur.User.IsDeleted)
            .Select(ur => ur.UserId)
            .Distinct();

    /// <summary>Lança se <paramref name="targetUserId"/> for o único Administrador ativo.</summary>
    public static async Task EnsureNotLastActiveAdministratorAsync(IApplicationDbContext db, Guid targetUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var admins = await ActiveAdministratorIds(db, now).ToListAsync(cancellationToken);
        if (admins.Contains(targetUserId) && admins.Count == 1)
            throw new BusinessRuleValidationException(LastAdministratorMessage);
    }

    public static ValidityState StateAt(IHasValidity item, DateTimeOffset now) =>
        item.ValidFrom is { } from && from > now ? ValidityState.Scheduled
        : item.IsExpiredAt(now) ? ValidityState.Expired
        : ValidityState.Active;
}

public sealed class UserAccessAdministrationService : IUserAccessAdministrationService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISecurityAuditLogger _securityAudit;

    public UserAccessAdministrationService(IApplicationDbContext db, IDateTimeProvider clock, ISecurityAuditLogger securityAudit)
    {
        _db = db;
        _clock = clock;
        _securityAudit = securityAudit;
    }

    public async Task<IReadOnlyList<RoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        // Só os perfis de sistema, na ordem da definição.
        return SystemRoles.All
            .Select(def => (def, role: roles.FirstOrDefault(r => r.Name == def.Name)))
            .Where(x => x.role is not null)
            .Select(x => new RoleOptionDto(x.role!.Id, x.def.Name, x.def.Label, x.def.Description, x.def.Permissions))
            .ToList();
    }

    public async Task<UserAccessDto> GetUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var now = _clock.UtcNow;

        var roles = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.Id, ur.RoleId, ur.Role.Name, ur.ValidFrom, ur.ValidUntil })
            .ToListAsync(cancellationToken);

        var units = await _db.UserManagementUnits.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.ManagementUnitId, x.ManagementUnit.Code, x.ManagementUnit.Name, x.ValidFrom, x.ValidUntil })
            .ToListAsync(cancellationToken);

        return new UserAccessDto(
            userId,
            roles
                .Select(r => new UserRoleDto(r.Id, r.RoleId, r.Name, SystemRoles.Find(r.Name)?.Label ?? r.Name, r.ValidFrom, r.ValidUntil,
                    UserAccessRules.StateAt(new Validity(r.ValidFrom, r.ValidUntil), now)))
                .OrderBy(r => r.RoleLabel, StringComparer.CurrentCulture)
                .ToList(),
            units
                .Select(u => new UserManagementUnitDto(u.Id, u.ManagementUnitId, u.Code, u.Name, u.ValidFrom, u.ValidUntil,
                    UserAccessRules.StateAt(new Validity(u.ValidFrom, u.ValidUntil), now)))
                .OrderBy(u => u.Code, StringComparer.Ordinal)
                .ToList());
    }

    public async Task<UserAccessDto> AssignRoleAsync(Guid actorUserId, Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        UserAccessRules.EnsureNotSelf(actorUserId, userId);
        ValidityExtensions.EnsureValidityRange(request.ValidFrom, request.ValidUntil);
        var now = _clock.UtcNow;
        var user = await LoadUserAsync(userId, cancellationToken);

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
                   ?? throw new BusinessRuleValidationException("Perfil não encontrado.");
        if (SystemRoles.Find(role.Name) is null)
            throw new BusinessRuleValidationException("Só os perfis de sistema podem ser atribuídos.");

        if (role.Name == SystemRoles.Manager && !await HasUsableManagementUnitAsync(userId, now, exceptLinkId: null, cancellationToken))
            throw new BusinessRuleValidationException(UserAccessRules.ManagerWithoutUnitMessage);

        var link = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);
        if (link is null)
        {
            link = new UserRole { UserId = userId, RoleId = role.Id, ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil };
            await _db.UserRoles.AddAsync(link, cancellationToken);
        }
        else
        {
            // Mesmo perfil de novo: só a validade muda. Limitar a validade do último administrador ativo seria retirá-lo.
            var stillValidNow = new Validity(request.ValidFrom, request.ValidUntil).IsValidAt(now);
            if (role.Name == SystemRoles.Administrator && !stillValidNow)
                await UserAccessRules.EnsureNotLastActiveAdministratorAsync(_db, userId, now, cancellationToken);
            link.ValidFrom = request.ValidFrom;
            link.ValidUntil = request.ValidUntil;
        }

        user.RotateSecurityStamp();
        await _db.SaveChangesAsync(cancellationToken);
        await _securityAudit.RecordAsync(SecurityAuditActions.RoleAssigned, actorUserId,
            new { targetUserId = userId, role = role.Name, validFrom = request.ValidFrom, validUntil = request.ValidUntil },
            cancellationToken, nameof(UserRole), link.Id.ToString());

        return await GetUserAccessAsync(userId, cancellationToken);
    }

    public async Task<UserAccessDto> RevokeRoleAsync(Guid actorUserId, Guid userId, Guid userRoleId, CancellationToken cancellationToken = default)
    {
        UserAccessRules.EnsureNotSelf(actorUserId, userId);
        var now = _clock.UtcNow;
        var user = await LoadUserAsync(userId, cancellationToken);

        var link = await _db.UserRoles.Include(ur => ur.Role)
                       .FirstOrDefaultAsync(ur => ur.Id == userRoleId && ur.UserId == userId, cancellationToken)
                   ?? throw new KeyNotFoundException("Vínculo de perfil não encontrado.");

        if (link.Role.Name == SystemRoles.Administrator)
            await UserAccessRules.EnsureNotLastActiveAdministratorAsync(_db, userId, now, cancellationToken);

        link.IsDeleted = true;
        link.DeletedAt = now;
        link.DeletedBy = actorUserId;
        user.RotateSecurityStamp();
        await _db.SaveChangesAsync(cancellationToken);
        await _securityAudit.RecordAsync(SecurityAuditActions.RoleRevoked, actorUserId,
            new { targetUserId = userId, role = link.Role.Name, validFrom = link.ValidFrom, validUntil = link.ValidUntil },
            cancellationToken, nameof(UserRole), link.Id.ToString());

        return await GetUserAccessAsync(userId, cancellationToken);
    }

    public async Task<UserAccessDto> AssignManagementUnitAsync(Guid actorUserId, Guid userId, AssignManagementUnitRequest request, CancellationToken cancellationToken = default)
    {
        UserAccessRules.EnsureNotSelf(actorUserId, userId);
        ValidityExtensions.EnsureValidityRange(request.ValidFrom, request.ValidUntil);
        var now = _clock.UtcNow;
        var user = await LoadUserAsync(userId, cancellationToken);

        var unit = await _db.ManagementUnits.FirstOrDefaultAsync(m => m.Id == request.ManagementUnitId, cancellationToken)
                   ?? throw new BusinessRuleValidationException("Gerência não encontrada.");

        var link = await _db.UserManagementUnits.FirstOrDefaultAsync(x => x.UserId == userId && x.ManagementUnitId == unit.Id, cancellationToken);
        if (link is null)
        {
            link = new UserManagementUnit { UserId = userId, ManagementUnitId = unit.Id, ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil };
            await _db.UserManagementUnits.AddAsync(link, cancellationToken);
        }
        else
        {
            if (new Validity(request.ValidFrom, request.ValidUntil).IsExpiredAt(now))
                await EnsureManagerKeepsAUnitAsync(userId, now, link.Id, cancellationToken);
            link.ValidFrom = request.ValidFrom;
            link.ValidUntil = request.ValidUntil;
        }

        user.RotateSecurityStamp();
        await _db.SaveChangesAsync(cancellationToken);
        await _securityAudit.RecordAsync(SecurityAuditActions.ManagementUnitAssigned, actorUserId,
            new { targetUserId = userId, managementUnit = unit.Code, validFrom = request.ValidFrom, validUntil = request.ValidUntil },
            cancellationToken, nameof(UserManagementUnit), link.Id.ToString());

        return await GetUserAccessAsync(userId, cancellationToken);
    }

    public async Task<UserAccessDto> RevokeManagementUnitAsync(Guid actorUserId, Guid userId, Guid userManagementUnitId, CancellationToken cancellationToken = default)
    {
        UserAccessRules.EnsureNotSelf(actorUserId, userId);
        var now = _clock.UtcNow;
        var user = await LoadUserAsync(userId, cancellationToken);

        var link = await _db.UserManagementUnits.Include(x => x.ManagementUnit)
                       .FirstOrDefaultAsync(x => x.Id == userManagementUnitId && x.UserId == userId, cancellationToken)
                   ?? throw new KeyNotFoundException("Vínculo de gerência não encontrado.");

        await EnsureManagerKeepsAUnitAsync(userId, now, link.Id, cancellationToken);

        link.IsDeleted = true;
        link.DeletedAt = now;
        link.DeletedBy = actorUserId;
        user.RotateSecurityStamp();
        await _db.SaveChangesAsync(cancellationToken);
        await _securityAudit.RecordAsync(SecurityAuditActions.ManagementUnitRevoked, actorUserId,
            new { targetUserId = userId, managementUnit = link.ManagementUnit.Code, validFrom = link.ValidFrom, validUntil = link.ValidUntil },
            cancellationToken, nameof(UserManagementUnit), link.Id.ToString());

        return await GetUserAccessAsync(userId, cancellationToken);
    }

    /// <summary>Quem tem o perfil Gestor (não vencido) não pode ficar sem nenhuma gerência (não vencida).</summary>
    private async Task EnsureManagerKeepsAUnitAsync(Guid userId, DateTimeOffset now, Guid removedLinkId, CancellationToken cancellationToken)
    {
        var isManager = await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.Role.Name == SystemRoles.Manager
                                                          && (ur.ValidUntil == null || ur.ValidUntil > now), cancellationToken);
        if (isManager && !await HasUsableManagementUnitAsync(userId, now, removedLinkId, cancellationToken))
            throw new BusinessRuleValidationException(UserAccessRules.ManagerWithoutUnitMessage + " Retire antes o perfil Gestor.");
    }

    private Task<bool> HasUsableManagementUnitAsync(Guid userId, DateTimeOffset now, Guid? exceptLinkId, CancellationToken cancellationToken) =>
        _db.UserManagementUnits.AnyAsync(x => x.UserId == userId && x.Id != exceptLinkId
                                              && (x.ValidUntil == null || x.ValidUntil > now), cancellationToken);

    private async Task<User> LoadUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
        ?? throw new KeyNotFoundException("Usuário não encontrado.");

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            throw new KeyNotFoundException("Usuário não encontrado.");
    }

    private sealed record Validity(DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil) : IHasValidity;
}

public sealed class ManagementUnitAdministrationService : IManagementUnitAdministrationService
{
    private readonly IApplicationDbContext _db;

    public ManagementUnitAdministrationService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ManagementUnitDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.ManagementUnits.AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new ManagementUnitDto(m.Id, m.Code, m.Name, m.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ManagementUnitDto> RenameAsync(Guid id, RenameManagementUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _db.ManagementUnits.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
                   ?? throw new KeyNotFoundException("Gerência não encontrada.");
        unit.UpdateName(request.Name ?? string.Empty);
        await _db.SaveChangesAsync(cancellationToken);
        return new ManagementUnitDto(unit.Id, unit.Code, unit.Name, unit.IsActive);
    }
}
