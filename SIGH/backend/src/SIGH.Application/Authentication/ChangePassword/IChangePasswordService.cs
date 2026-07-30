namespace SIGH.Application.Authentication.ChangePassword;

public interface IChangePasswordService
{
    Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
