namespace SIGH.Application.Authentication.ResetPassword;

public record ResetPasswordRequest(
    string Email,
    string ResetToken,
    string NewPassword
);

public record ResetPasswordResponse(
    bool Success,
    string Message
);
