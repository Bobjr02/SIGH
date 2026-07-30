namespace SIGH.Application.Authentication.Logout;

public interface ILogoutService
{
    Task<LogoutResponse> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}
