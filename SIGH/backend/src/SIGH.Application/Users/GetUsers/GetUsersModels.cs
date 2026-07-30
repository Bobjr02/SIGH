using SIGH.Domain.Enums;

namespace SIGH.Application.Users.GetUsers;

public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    UserStatus? Status = null
);

public record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    string Cpf,
    string Status,
    bool MustChangePassword,
    List<string> Roles,
    DateTimeOffset CreatedAt
);

public record PagedList<T>(
    List<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
