using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.Logout;
using SIGH.Application.Authentication.RefreshToken;

namespace SIGH.Api.Contracts.Auth;

// Contratos HTTP de autenticação:
// - não expõem o ID do usuário nas requisições (o usuário é sempre o dono do token);
// - não trazem o refresh token no corpo (ele vai só no cookie HttpOnly sigh_rt).

public record LogoutHttpRequest(Guid? SessionId = null)
{
    public LogoutRequest ToApplicationRequest(Guid actorUserId, string? refreshToken) =>
        new(actorUserId, refreshToken, SessionId);
}

public record ChangePasswordHttpRequest(string CurrentPassword, string NewPassword)
{
    public ChangePasswordRequest ToApplicationRequest(Guid actorUserId, string? ipAddress = null, string? userAgent = null) =>
        new(actorUserId, CurrentPassword, NewPassword, ipAddress, userAgent);
}

public record LoginHttpResponse(
    string AccessToken,
    int ExpiresInMinutes,
    bool MustChangePassword,
    UserDto User)
{
    public static LoginHttpResponse From(LoginResponse response) =>
        new(response.AccessToken, response.ExpiresInMinutes, response.MustChangePassword, response.User);
}

public record RefreshHttpResponse(string AccessToken, int ExpiresInMinutes)
{
    public static RefreshHttpResponse From(RefreshTokenResponse response) =>
        new(response.AccessToken, response.ExpiresInMinutes);
}

public record ChangePasswordHttpResponse(bool Success, string Message, string AccessToken, int ExpiresInMinutes)
{
    public static ChangePasswordHttpResponse From(ChangePasswordResponse response) =>
        new(response.Success, response.Message, response.AccessToken, response.ExpiresInMinutes);
}
