namespace SIGH.Application.Users.GetUsers;

public class GetUsersService : IGetUsersService
{
    private readonly IUserQueryService _queryService;

    public GetUsersService(IUserQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<PagedList<UserSummaryDto>> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        return await _queryService.GetUsersPagedAsync(query, cancellationToken);
    }
}
