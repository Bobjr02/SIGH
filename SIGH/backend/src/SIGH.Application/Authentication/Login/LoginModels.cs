namespace SIGH.Application.Authentication.Login;

public record LoginRequest(
    string Email,
    string Password,
    string? DeviceName = null,
    string? Browser = null,
    string? OperatingSystem = null,
    string? IpAddress = null,
    string? UserAgent = null
);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Status,
    bool MustChangePassword,
    List<string> Roles,
    List<string> Permissions
);

/// <summary>
/// UserDto não traz CPF: respostas de autenticação não expõem dado pessoal além do necessário.
/// O RefreshToken é entregue ao cliente só via cookie HttpOnly pela camada HTTP.
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes,
    bool MustChangePassword,
    UserDto User,
    DateTimeOffset RefreshTokenExpiresAt
);
