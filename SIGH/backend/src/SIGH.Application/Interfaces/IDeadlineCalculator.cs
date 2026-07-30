namespace SIGH.Application.Interfaces;

using SIGH.Application.Notifications.DTOs;

public interface IDeadlineCalculator
{
    DateTime CalculateDueDate(DateTime startDate, int businessDays, IEnumerable<DateTime>? holidays = null);
    bool IsDeadlineExpired(DateTime dueDate, DateTime? referenceDate = null);
    TimeSpan GetRemainingTime(DateTime dueDate, DateTime? referenceDate = null);
    int GetRemainingDays(DateTime dueDate, DateTime? referenceDate = null);
    int GetOverdueDays(DateTime dueDate, DateTime? referenceDate = null);
    DeadlineCalculationResultDto CalculateDeadlineDetails(DeadlineCalculationRequestDto request, IEnumerable<DateTime>? holidays = null);
}
