namespace SIGH.Application.Disciplinary.Reports.DTOs;

public class EmployeeDisciplinaryHistoryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int ConcludedCases { get; set; }
    public int CancelledCases { get; set; }
    public int TotalMeasures { get; set; }
    public DateTimeOffset? MostRecentCaseDate { get; set; }
    public IReadOnlyCollection<EmployeeDisciplinaryCaseSummaryDto> Cases { get; set; } = Array.Empty<EmployeeDisciplinaryCaseSummaryDto>();
}

public class EmployeeDisciplinaryCaseSummaryDto
{
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ConcludedAt { get; set; }
    public string ResponsibleEmployee { get; set; } = string.Empty;
    public int OccurrencesCount { get; set; }
    public int DecisionsCount { get; set; }
    public int MeasuresCount { get; set; }
    /// <summary>"SIGH" ou "Histórico".</summary>
    public string Origin { get; set; } = "SIGH";
    /// <summary>Histórico: data do lote que importou ("Histórico importado — lote de dd/MM/yyyy").</summary>
    public DateTimeOffset? ImportedAt { get; set; }
    /// <summary>Data da medida (histórico) ou da abertura (SIGH): ordena a linha do tempo.</summary>
    public DateTimeOffset OccurredAt { get; set; }
}
