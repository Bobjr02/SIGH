namespace SIGH.Application.Notifications.Queries;

public class GetNotificationsQuery
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsRead { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortDirection { get; set; } = "desc";
}
