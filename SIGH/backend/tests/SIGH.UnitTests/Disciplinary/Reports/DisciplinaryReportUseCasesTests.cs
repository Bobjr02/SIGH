using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Reports.DTOs;
using SIGH.Application.Disciplinary.Reports.Options;
using SIGH.Application.Disciplinary.Reports.Queries;
using SIGH.Application.Disciplinary.Reports.UseCases;
using SIGH.Application.Disciplinary.Reports.Utils;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.Reports;

public class DisciplinaryReportUseCasesTests
{
    private readonly Mock<IEmployeeDisciplinaryHistoryQueryRepository> _historyRepoMock = new();
    private readonly Mock<IDisciplinaryDashboardQueryRepository> _dashboardRepoMock = new();
    private readonly Mock<IDisciplinaryReportQueryRepository> _reportRepoMock = new();
    private readonly Mock<IAuthorizedCompanyProvider> _authorizedCompanyProviderMock = new();

    [Fact]
    public async Task GetEmployeeDisciplinaryHistory_ShouldFail_WhenEmployeeIdIsEmpty()
    {
        var query = new GetEmployeeDisciplinaryHistoryQuery { EmployeeId = Guid.Empty, CompanyId = Guid.NewGuid() };
        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(query.CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(query.CompanyId);
        var useCase = new GetEmployeeDisciplinaryHistoryUseCase(_historyRepoMock.Object, _authorizedCompanyProviderMock.Object);

        var result = await useCase.ExecuteAsync(query);

        Assert.False(result.Success);
        Assert.Equal("INVALID_EMPLOYEE_ID", result.ErrorCode);
    }

    [Fact]
    public async Task GetEmployeeDisciplinaryHistory_ShouldReturnOk_WhenEmployeeExists()
    {
        var empId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var mockDto = new EmployeeDisciplinaryHistoryDto
        {
            EmployeeId = empId,
            EmployeeName = "João Silva",
            CompanyId = compId,
            TotalCases = 2,
            OpenCases = 1
        };

        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(compId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compId);
        _historyRepoMock.Setup(r => r.GetEmployeeHistoryAsync(It.IsAny<GetEmployeeDisciplinaryHistoryQuery>(), compId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        var useCase = new GetEmployeeDisciplinaryHistoryUseCase(_historyRepoMock.Object, _authorizedCompanyProviderMock.Object);
        var query = new GetEmployeeDisciplinaryHistoryQuery { EmployeeId = empId, CompanyId = compId };

        var result = await useCase.ExecuteAsync(query);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("João Silva", result.Data!.EmployeeName);
        Assert.Equal(2, result.Data.TotalCases);
    }

    [Fact]
    public async Task GetDisciplinaryDashboard_ShouldFail_WhenCompanyIdIsEmpty()
    {
        var query = new GetDisciplinaryDashboardQuery { CompanyId = Guid.Empty };
        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(query.CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);
        var useCase = new GetDisciplinaryDashboardUseCase(_dashboardRepoMock.Object, _authorizedCompanyProviderMock.Object);

        var result = await useCase.ExecuteAsync(query);

        Assert.False(result.Success);
        Assert.Equal("INVALID_COMPANY_ID", result.ErrorCode);
    }

    [Fact]
    public async Task GetDisciplinaryDashboard_ShouldReturnOk_WhenValidCompany()
    {
        var compId = Guid.NewGuid();
        var mockDash = new DisciplinaryDashboardDto
        {
            TotalCases = 10,
            ConcludedCases = 5,
            AverageResolutionTimeInDays = 3.5
        };

        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(compId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compId);
        _dashboardRepoMock.Setup(r => r.GetDashboardAsync(It.IsAny<GetDisciplinaryDashboardQuery>(), compId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDash);

        var useCase = new GetDisciplinaryDashboardUseCase(_dashboardRepoMock.Object, _authorizedCompanyProviderMock.Object);
        var query = new GetDisciplinaryDashboardQuery { CompanyId = compId };

        var result = await useCase.ExecuteAsync(query);

        Assert.True(result.Success);
        Assert.Equal(10, result.Data!.TotalCases);
        Assert.Equal(3.5, result.Data.AverageResolutionTimeInDays);
    }

    [Fact]
    public async Task GetDisciplinaryCaseReport_ShouldReject_InvalidSortField()
    {
        var query = new GetDisciplinaryCaseReportQuery
        {
            CompanyId = Guid.NewGuid(),
            SortBy = "DROP TABLE DisciplinaryCases;--"
        };
        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(query.CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(query.CompanyId);
        var useCase = new GetDisciplinaryCaseReportUseCase(_reportRepoMock.Object, _authorizedCompanyProviderMock.Object);

        var result = await useCase.ExecuteAsync(query);

        Assert.False(result.Success);
        Assert.Equal("INVALID_SORT_FIELD", result.ErrorCode);
    }

    [Fact]
    public async Task ExportDisciplinaryCaseReportCsv_ShouldSanitizeCsvInjection()
    {
        // Test sanitization helper
        Assert.Equal("\"'=1+1\"", CsvSanitizer.SanitizeField(" =1+1"));
        Assert.Equal("\"'+cmd.exe\"", CsvSanitizer.SanitizeField("+cmd.exe"));
        Assert.Equal("\"'-SUM(A1:A10)\"", CsvSanitizer.SanitizeField("-SUM(A1:A10)"));
        Assert.Equal("\"'@eval()\"", CsvSanitizer.SanitizeField("@eval()"));
        Assert.Equal("\"Texto \"\"Normal\"\"\"", CsvSanitizer.SanitizeField("Texto \"Normal\""));
    }

    [Fact]
    public async Task ExportDisciplinaryCaseReportCsv_ShouldGenerateValidCsvBytes()
    {
        var compId = Guid.NewGuid();
        var items = new List<DisciplinaryCaseReportItemDto>
        {
            new()
            {
                CaseNumber = "PROC-2026-001",
                Title = "Atraso Reincidente",
                CompanyName = "Empresa A",
                Status = "Opened",
                Priority = "Normal",
                OpenedAt = DateTimeOffset.UtcNow
            }
        };

        _authorizedCompanyProviderMock
            .Setup(p => p.GetAuthorizedCompanyIdAsync(compId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compId);
        _reportRepoMock.Setup(r => r.GetCasesForExportAsync(It.IsAny<ExportDisciplinaryCaseReportCsvQuery>(), compId, 1001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var options = Options.Create(new DisciplinaryReportOptions
        {
            MaximumExportRecords = 1000,
            CsvSeparator = ";"
        });
        var useCase = new ExportDisciplinaryCaseReportCsvUseCase(_reportRepoMock.Object, _authorizedCompanyProviderMock.Object, options);
        var query = new ExportDisciplinaryCaseReportCsvQuery { CompanyId = compId };

        var result = await useCase.ExecuteAsync(query);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        var csvString = Encoding.UTF8.GetString(result.Data!);
        Assert.Contains("Número do Processo;Título;Empresa", csvString);
        Assert.Contains("PROC-2026-001", csvString);
    }
}
