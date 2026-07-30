using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Reports.Queries;

public class GetEmployeeDisciplinaryHistoryQuery
{
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public DisciplinaryCaseStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetDisciplinaryDashboardQuery
{
    public Guid CompanyId { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public Guid? DepartmentId { get; set; }
    public DisciplinaryCaseStatus? Status { get; set; }
    public DisciplinaryCasePriority? Priority { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
}

public class GetDisciplinaryMeasuresQuery
{
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CaseId { get; set; }
    public string? MeasureType { get; set; }
    public DateTimeOffset? AppliedFrom { get; set; }
    public DateTimeOffset? AppliedTo { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetDisciplinaryCaseReportQuery
{
    public Guid CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public Guid? InfractionTypeId { get; set; }
    public DisciplinaryCaseStatus? Status { get; set; }
    public DisciplinaryCasePriority? Priority { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
}

public class ExportDisciplinaryCaseReportCsvQuery : GetDisciplinaryCaseReportQuery
{
    public int MaxRecords { get; set; } = 1000;
}
