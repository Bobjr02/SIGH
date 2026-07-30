namespace SIGH.Application.Users.UnlockUser;

public interface IUnlockUserService
{
    Task<UnlockUserResponse> UnlockUserAsync(UnlockUserRequest request, CancellationToken cancellationToken = default);
}
