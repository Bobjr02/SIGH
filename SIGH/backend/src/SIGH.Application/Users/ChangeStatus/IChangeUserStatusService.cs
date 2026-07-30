namespace SIGH.Application.Users.ChangeStatus;

public interface IChangeUserStatusService
{
    Task<ChangeUserStatusResponse> ChangeUserStatusAsync(ChangeUserStatusRequest request, CancellationToken cancellationToken = default);
}
