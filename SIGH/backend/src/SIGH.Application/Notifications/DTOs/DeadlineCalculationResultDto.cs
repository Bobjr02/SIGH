namespace SIGH.Application.Notifications.DTOs;

public class DeadlineCalculationResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int TotalDays { get; set; }
    public int BusinessDaysCount { get; set; }
    public bool IsExpired { get; set; }
}
