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
    string Cpf,
    string Status,
    bool MustChangePassword,
    List<string> Roles,
    List<string> Permissions
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes,
    bool MustChangePassword,
    UserDto User
);
