using Microsoft.EntityFrameworkCore;
using SIGH.Application.Users.GetUsers;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class UserQueryService : IUserQueryService
{
    private readonly SighDbContext _context;

    public UserQueryService(SighDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<UserSummaryDto>> GetUsersPagedAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.Cpf.Contains(term));
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.Status == query.Status.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var users = await dbQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userDtos = users.Select(u => new UserSummaryDto(
            u.Id,
            u.FullName,
            u.Email,
            u.Cpf,
            u.Status.ToString(),
            u.MustChangePassword,
            u.UserRoles
                .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToList(),
            u.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedList<UserSummaryDto>(
            userDtos,
            pageNumber,
            pageSize,
            totalCount,
            totalPages
        );
    }
}
