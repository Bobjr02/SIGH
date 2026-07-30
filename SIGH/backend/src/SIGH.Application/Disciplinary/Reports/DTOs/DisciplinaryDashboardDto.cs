namespace SIGH.Application.Disciplinary.Reports.DTOs;

public class DashboardDistributionItemDto
{
    public string? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public double Percentage { get; set; }
}

public class DisciplinaryDashboardDto
{
    public int TotalCases { get; set; }
    public int DraftCases { get; set; }
    public int OpenCases { get; set; }
    public int UnderInvestigationCases { get; set; }
    public int AwaitingDecisionCases { get; set; }
    public int DecidedCases { get; set; }
    public int ConcludedCases { get; set; }
    public int CancelledCases { get; set; }
    public double AverageResolutionTimeInDays { get; set; }
    public int CasesOpenedInPeriod { get; set; }
    public int CasesConcludedInPeriod { get; set; }
    public int EmployeesWithCases { get; set; }
    public int MeasuresApplied { get; set; }

    public IReadOnlyCollection<DashboardDistributionItemDto> CasesByStatus { get; set; } = Array.Empty<DashboardDistributionItemDto>();
    public IReadOnlyCollection<DashboardDistributionItemDto> CasesByPriority { get; set; } = Array.Empty<DashboardDistributionItemDto>();
    public IReadOnlyCollection<DashboardDistributionItemDto> CasesByMonth { get; set; } = Array.Empty<DashboardDistributionItemDto>();
    public IReadOnlyCollection<DashboardDistributionItemDto> CasesByInfractionType { get; set; } = Array.Empty<DashboardDistributionItemDto>();
    public IReadOnlyCollection<DashboardDistributionItemDto> MeasuresByType { get; set; } = Array.Empty<DashboardDistributionItemDto>();
    public IReadOnlyCollection<DashboardDistributionItemDto> CasesByDepartment { get; set; } = Array.Empty<DashboardDistributionItemDto>();
}
