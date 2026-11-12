using SIGH.Domain.Entities;

namespace SIGH.Application.Authentication.Common;

/// <summary>
/// Perfis e permissões ativos de um usuário carregado com UserRoles → Role → RolePermissions → Permission.
/// Fonte única para as claims emitidas no login, no refresh e na troca de senha.
/// </summary>
public static class UserAccessExtensions
{
    public static List<string> GetActiveRoleNames(this User user) =>
        user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

    public static List<string> GetActivePermissionCodes(this User user) =>
        user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => !rp.IsDeleted && rp.Permission != null && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();
}
