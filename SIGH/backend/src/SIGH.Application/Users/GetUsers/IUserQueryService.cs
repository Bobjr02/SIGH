using SIGH.Application.Common.Models;

namespace SIGH.Application.Users.GetUsers;

public interface IUserQueryService
{
    Task<PagedList<UserSummaryDto>> GetUsersPagedAsync(GetUsersQuery query, CancellationToken cancellationToken = default);
}
