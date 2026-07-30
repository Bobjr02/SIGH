namespace SIGH.Application.Users.CreateUser;

public record CreateUserRequest(
    string FullName,
    string Email,
    string Cpf,
    string InitialPassword,
    List<Guid> RoleIds
);

public record CreateUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Cpf,
    string Status,
    bool MustChangePassword
);
