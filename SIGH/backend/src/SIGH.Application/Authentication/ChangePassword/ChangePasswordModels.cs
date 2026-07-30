namespace SIGH.Application.Authentication.ChangePassword;

public record ChangePasswordRequest(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
);

public record ChangePasswordResponse(
    bool Success,
    string Message
);
