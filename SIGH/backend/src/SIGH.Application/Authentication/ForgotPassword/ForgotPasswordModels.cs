namespace SIGH.Application.Authentication.ForgotPassword;

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(bool Success, string Message);
