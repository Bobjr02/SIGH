namespace SIGH.Application.Users.CreateUser;

/// <summary>
/// InitialPassword é opcional e desaconselhado: o usuário recebe por e-mail um link de uso único para
/// definir a própria senha. Se o administrador informar uma senha inicial, ela NUNCA vai no e-mail e
/// precisa ser comunicada por outro canal; a troca continua obrigatória no primeiro login.
/// </summary>
public record CreateUserRequest(
    string FullName,
    string Email,
    string Cpf,
    string? InitialPassword,
    List<Guid> RoleIds,
    // Gerências (obrigatório se um dos perfis for Gestor).
    List<Guid>? ManagementUnitIds = null
);

public record CreateUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Cpf,
    string Status,
    bool MustChangePassword
);
