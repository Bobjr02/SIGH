namespace SIGH.Application.Users.GetUserById;

public record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string Cpf,
    string Status,
    bool MustChangePassword,
    int FailedLoginAttempts,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset? LastLoginAt,
    List<string> Roles,
    List<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
