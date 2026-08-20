using FluentAssertions;
using Moq;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.GetEmployees;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class GetEmployeesUseCaseTests
{
    private readonly Mock<IEmployeeQueryService> _queryServiceMock;
    private readonly GetEmployeesUseCase _useCase;

    public GetEmployeesUseCaseTests()
    {
        _queryServiceMock = new Mock<IEmployeeQueryService>();
        _useCase = new GetEmployeesUseCase(_queryServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithFilterHasSystemAccessTrue_ShouldPassFilterToQueryServiceAndReturnResults()
    {
        // Arrange
        var query = new GetEmployeesQuery(HasSystemAccess: true);
        var pagedList = new PagedResult<EmployeeListItemResponse>(
            items: new List<EmployeeListItemResponse>
            {
                new EmployeeListItemResponse(
                    Id: Guid.NewGuid(),
                    CompanyId: Guid.NewGuid(),
                    CompanyName: "Empresa SIGH",
                    EmployeeNumber: "EMP-001",
                    FullName: "João Com Acesso",
                    SocialName: null,
                    CpfMasked: "***.***.***-25",
                    AdmissionDate: new DateOnly(2025, 1, 1),
                    Status: Domain.Employees.Enums.EmployeeStatus.Active,
                    JobTitleId: Guid.NewGuid(),
                    JobTitleName: "Dev",
                    ManagementUnitId: Guid.NewGuid(),
                    ManagementUnitName: "UG01",
                    DepartmentId: null,
                    DepartmentName: null,
                    SupervisorId: null,
                    SupervisorName: null,
                    UserId: Guid.NewGuid(),
                    HasSystemAccess: true,
                    CorporateEmail: "joao@sigh.com",
                    CreatedAt: DateTimeOffset.UtcNow
                )
            },
            pageNumber: 1,
            pageSize: 20,
            totalCount: 1
        );

        _queryServiceMock
            .Setup(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.HasSystemAccess == true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().HasSystemAccess.Should().BeTrue();
        result.Data.Items.First().UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithFilterHasSystemAccessFalse_ShouldPassFilterToQueryServiceAndReturnResults()
    {
        // Arrange
        var query = new GetEmployeesQuery(HasSystemAccess: false);
        var pagedList = new PagedResult<EmployeeListItemResponse>(
            items: new List<EmployeeListItemResponse>
            {
                new EmployeeListItemResponse(
                    Id: Guid.NewGuid(),
                    CompanyId: Guid.NewGuid(),
                    CompanyName: "Empresa SIGH",
                    EmployeeNumber: "EMP-002",
                    FullName: "Maria Sem Acesso",
                    SocialName: null,
                    CpfMasked: "***.***.***-25",
                    AdmissionDate: new DateOnly(2025, 1, 1),
                    Status: Domain.Employees.Enums.EmployeeStatus.Active,
                    JobTitleId: Guid.NewGuid(),
                    JobTitleName: "Dev",
                    ManagementUnitId: Guid.NewGuid(),
                    ManagementUnitName: "UG01",
                    DepartmentId: null,
                    DepartmentName: null,
                    SupervisorId: null,
                    SupervisorName: null,
                    UserId: null,
                    HasSystemAccess: false,
                    CorporateEmail: "maria@sigh.com",
                    CreatedAt: DateTimeOffset.UtcNow
                )
            },
            pageNumber: 1,
            pageSize: 20,
            totalCount: 1
        );

        _queryServiceMock
            .Setup(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.HasSystemAccess == false), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().HasSystemAccess.Should().BeFalse();
        result.Data.Items.First().UserId.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithAdmissionDatePeriod_ShouldPassPeriodFiltersToQueryService()
    {
        // Arrange
        var fromDate = new DateOnly(2025, 1, 1);
        var toDate = new DateOnly(2025, 12, 31);
        var query = new GetEmployeesQuery(AdmissionDateFrom: fromDate, AdmissionDateTo: toDate);

        var pagedList = new PagedResult<EmployeeListItemResponse>(
            items: new List<EmployeeListItemResponse>(),
            pageNumber: 1,
            pageSize: 20,
            totalCount: 0
        );

        _queryServiceMock
            .Setup(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.AdmissionDateFrom == fromDate && q.AdmissionDateTo == toDate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _queryServiceMock.Verify(q => q.SearchAsync(It.IsAny<GetEmployeesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSupervisorIdFilter_ShouldPassSupervisorFilterToQueryService()
    {
        // Arrange
        var supervisorId = Guid.NewGuid();
        var query = new GetEmployeesQuery(SupervisorId: supervisorId);

        var pagedList = new PagedResult<EmployeeListItemResponse>(
            items: new List<EmployeeListItemResponse>(),
            pageNumber: 1,
            pageSize: 20,
            totalCount: 0
        );

        _queryServiceMock
            .Setup(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.SupervisorId == supervisorId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _queryServiceMock.Verify(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.SupervisorId == supervisorId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSearchTermIgnoringCpf_ShouldPassEffectiveSearchTermToQueryService()
    {
        // Arrange
        var query = new GetEmployeesQuery(SearchTerm: "João");

        var pagedList = new PagedResult<EmployeeListItemResponse>(
            items: new List<EmployeeListItemResponse>(),
            pageNumber: 1,
            pageSize: 20,
            totalCount: 0
        );

        _queryServiceMock
            .Setup(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.EffectiveSearchTerm == "João"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _queryServiceMock.Verify(q => q.SearchAsync(It.Is<GetEmployeesQuery>(q => q.EffectiveSearchTerm == "João"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
