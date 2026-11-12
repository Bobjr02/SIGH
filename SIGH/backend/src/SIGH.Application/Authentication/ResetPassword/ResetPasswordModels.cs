namespace SIGH.Application.Authentication.ResetPassword;

/// <summary>O token (do link do e-mail) identifica o usuário: não há e-mail na requisição.</summary>
public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public record ResetPasswordResponse(
    bool Success,
    string Message
);
