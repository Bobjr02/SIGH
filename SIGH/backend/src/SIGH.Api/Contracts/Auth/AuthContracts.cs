using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.Logout;

namespace SIGH.Api.Contracts.Auth;

// Contratos HTTP de autenticação que não expõem o ID do usuário:
// o usuário é sempre o dono do token autenticado.

public record LogoutHttpRequest(string? RefreshToken = null, Guid? SessionId = null)
{
    public LogoutRequest ToApplicationRequest(Guid actorUserId) =>
        new(actorUserId, RefreshToken, SessionId);
}

public record ChangePasswordHttpRequest(string CurrentPassword, string NewPassword)
{
    public ChangePasswordRequest ToApplicationRequest(Guid actorUserId) =>
        new(actorUserId, CurrentPassword, NewPassword);
}
