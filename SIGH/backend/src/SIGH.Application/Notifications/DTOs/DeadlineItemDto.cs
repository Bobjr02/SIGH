namespace SIGH.Application.Notifications.DTOs;

public class DeadlineItemDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string SourceEntity { get; set; } = "DisciplinaryCase";
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset DueDate { get; set; }
    public Guid OpenedByUserId { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public Guid? DirectSupervisorUserId { get; set; }
    public Guid? HigherSupervisorUserId { get; set; }
}
