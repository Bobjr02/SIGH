namespace SIGH.Application.Authentication.RefreshToken;

public interface IRefreshTokenService
{
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
