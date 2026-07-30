namespace SIGH.Application.Disciplinary.Reports.DTOs;

public class DisciplinaryMeasureSummaryDto
{
    public Guid MeasureId { get; set; }
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string MeasureType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public Guid AppliedByEmployeeId { get; set; }
    public string AppliedByEmployeeName { get; set; } = string.Empty;
}

public class DisciplinaryCaseReportItemDto
{
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string ResponsibleEmployeeName { get; set; } = string.Empty;
    public string MainAccusedEmployeeName { get; set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ConcludedAt { get; set; }
    public double? ResolutionTimeInDays { get; set; }
    public int OccurrencesCount { get; set; }
    public int EvidencesCount { get; set; }
    public int DecisionsCount { get; set; }
    public int MeasuresCount { get; set; }
}
