namespace SIGH.Application.Users.GetUsers;

public interface IGetUsersService
{
    Task<PagedList<UserSummaryDto>> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken = default);
}
