namespace SIGH.Application.Authentication.Logout;

public record LogoutRequest(
    Guid UserId,
    string? RefreshToken = null,
    Guid? SessionId = null
);

public record LogoutResponse(
    bool Success,
    string Message
);
