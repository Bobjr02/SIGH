namespace SIGH.Infrastructure.Services;

using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;

public class DeadlineCalculator : IDeadlineCalculator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeadlineCalculator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public DateTime CalculateDueDate(DateTime startDate, int businessDays, IEnumerable<DateTime>? holidays = null)
    {
        if (businessDays <= 0)
            return startDate;

        var holidayList = holidays?.Select(h => h.Date).ToHashSet() ?? new HashSet<DateTime>();
        var current = startDate.Date;
        var addedDays = 0;

        while (addedDays < businessDays)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek != DayOfWeek.Saturday &&
                current.DayOfWeek != DayOfWeek.Sunday &&
                !holidayList.Contains(current))
            {
                addedDays++;
            }
        }

        return current;
    }

    public bool IsDeadlineExpired(DateTime dueDate, DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? _dateTimeProvider.UtcNow;
        return refDate > dueDate;
    }

    public TimeSpan GetRemainingTime(DateTime dueDate, DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? _dateTimeProvider.UtcNow;
        var diff = dueDate - refDate;
        return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
    }

    public int GetRemainingDays(DateTime dueDate, DateTime? referenceDate = null)
    {
        var remaining = GetRemainingTime(dueDate, referenceDate);
        return (int)Math.Ceiling(remaining.TotalDays);
    }

    public int GetOverdueDays(DateTime dueDate, DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? _dateTimeProvider.UtcNow;
        if (refDate <= dueDate)
            return 0;

        var diff = refDate - dueDate;
        return (int)Math.Ceiling(diff.TotalDays);
    }

    public DeadlineCalculationResultDto CalculateDeadlineDetails(DeadlineCalculationRequestDto request, IEnumerable<DateTime>? holidays = null)
    {
        var dueDate = CalculateDueDate(request.StartDate, request.BusinessDays, holidays);
        var totalDays = (dueDate - request.StartDate).Days;
        var isExpired = IsDeadlineExpired(dueDate);

        return new DeadlineCalculationResultDto
        {
            StartDate = request.StartDate,
            DueDate = dueDate,
            TotalDays = totalDays,
            BusinessDaysCount = request.BusinessDays,
            IsExpired = isExpired
        };
    }
}
