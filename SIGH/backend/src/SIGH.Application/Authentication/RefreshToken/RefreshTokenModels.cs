namespace SIGH.Application.Authentication.RefreshToken;

public record RefreshTokenRequest(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null
);

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes
);
