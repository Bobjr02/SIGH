namespace SIGH.Application.Authentication.CurrentUser;

/// <summary>
/// Dados do usuário autenticado (GET /api/v1/auth/me). Perfis e permissões são lidos
/// do banco a cada chamada, não do token, para refletir alterações recentes.
/// </summary>
public record CurrentUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Status,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
