namespace SIGH.Application.Authentication.ResetPassword;

public interface IResetPasswordService
{
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
