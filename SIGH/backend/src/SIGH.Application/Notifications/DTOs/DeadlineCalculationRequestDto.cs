namespace SIGH.Application.Notifications.DTOs;

public class DeadlineCalculationRequestDto
{
    public DateTime StartDate { get; set; }
    public int BusinessDays { get; set; }
    public bool IncludeHolidays { get; set; } = false;
}
