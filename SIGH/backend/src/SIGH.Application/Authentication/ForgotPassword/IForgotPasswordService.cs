namespace SIGH.Application.Authentication.ForgotPassword;

public interface IForgotPasswordService
{
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
}
