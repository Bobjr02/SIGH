namespace SIGH.Application.Notifications.DTOs;

public class DeadlineProcessingResultDto
{
    public int TotalItemsProcessed { get; set; }
    public int RemindersGenerated { get; set; }
    public int EscalationsExecuted { get; set; }
    public int ItemsSkippedDueToDuplicity { get; set; }
    public int FailedItemsCount { get; set; }
    public double ExecutionDurationMilliseconds { get; set; }
    public List<string> ExecutionLogs { get; set; } = new();
}
