namespace SIGH.Application.Disciplinary.Reports.Options;

public sealed class DisciplinaryReportOptions
{
    public const string SectionName = "DisciplinaryReports";

    public int MaximumExportRecords { get; init; } = 10_000;
    public string CsvSeparator { get; init; } = ";";
}
