namespace SIGH.Application.Users.GetUserById;

public interface IGetUserByIdService
{
    Task<UserDetailDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
