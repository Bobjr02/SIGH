namespace SIGH.Application.Authentication.CurrentUser;

public interface IGetCurrentUserService
{
    Task<CurrentUserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
