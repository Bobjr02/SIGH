namespace SIGH.Application.Authentication.ChangePassword;

public record ChangePasswordRequest(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string? IpAddress = null,
    string? UserAgent = null
);

/// <summary>
/// Após a troca, os refresh tokens anteriores do usuário são revogados e um novo par
/// de tokens é emitido, já sem a claim <c>pwd_change_required</c>. O cliente deve
/// substituir os tokens que tinha pelos devolvidos aqui.
/// </summary>
public record ChangePasswordResponse(
    bool Success,
    string Message,
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes
);
