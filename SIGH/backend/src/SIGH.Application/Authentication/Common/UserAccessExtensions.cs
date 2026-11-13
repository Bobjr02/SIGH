using SIGH.Domain.Entities;

namespace SIGH.Application.Authentication.Common;

/// <summary>
/// Perfis e permissões EFETIVOS de um usuário carregado com UserRoles → Role → RolePermissions → Permission
/// (e ManagementUnits, para o próximo vencimento). Só contam os vínculos vigentes em <c>now</c>:
/// um perfil com ValidUntil no passado (ou ValidFrom no futuro) não entra no token.
/// Fonte única para as claims emitidas no login, no refresh, na troca de senha e em /auth/me.
/// </summary>
public static class UserAccessExtensions
{
    private static IEnumerable<UserRole> ValidRoles(User user, DateTimeOffset now) =>
        user.UserRoles.Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted && ur.IsValidAt(now));

    public static List<string> GetActiveRoleNames(this User user, DateTimeOffset now) =>
        ValidRoles(user, now)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

    public static List<string> GetActivePermissionCodes(this User user, DateTimeOffset now) =>
        ValidRoles(user, now)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => !rp.IsDeleted && rp.Permission != null && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

    /// <summary>
    /// Próximo instante em que um perfil ou uma gerência do usuário começa ou termina. O token carrega
    /// esse instante; passado ele, a API responde 401 e o front renova com as permissões recalculadas.
    /// </summary>
    public static DateTimeOffset? GetNextAccessChange(this User user, DateTimeOffset now) =>
        user.UserRoles.Where(ur => !ur.IsDeleted).Cast<IHasValidity>()
            .Concat(user.ManagementUnits.Where(m => !m.IsDeleted))
            .Select(v => v.NextChangeAfter(now))
            .Where(d => d is not null)
            .Min();
}
