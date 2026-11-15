namespace SIGH.Application.Authentication.Login;

public record LoginRequest(
    string Email,
    string Password,
    string? DeviceName = null,
    string? Browser = null,
    string? OperatingSystem = null,
    string? IpAddress = null,
    string? UserAgent = null,
    /// <summary>"Lembrar-me". Nulo (clientes antigos) = true, o comportamento de antes.</summary>
    bool? RememberMe = null
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
    DateTimeOffset RefreshTokenExpiresAt,
    /// <summary>Falso: a camada HTTP grava o cookie do refresh token como cookie de sessão.</summary>
    bool IsPersistent = true
);
