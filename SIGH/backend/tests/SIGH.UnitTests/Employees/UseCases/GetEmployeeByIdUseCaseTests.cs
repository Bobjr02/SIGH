using FluentAssertions;
using Moq;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.GetEmployeeById;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class GetEmployeeByIdUseCaseTests
{
    private readonly Mock<IEmployeeQueryService> _queryServiceMock;
    private readonly GetEmployeeByIdUseCase _useCase;

    public GetEmployeeByIdUseCaseTests()
    {
        _queryServiceMock = new Mock<IEmployeeQueryService>();
        _useCase = new GetEmployeeByIdUseCase(_queryServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExists_ShouldReturnEmployeeDetails()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var details = new GetEmployeeByIdResponse(
            Id: employeeId,
            CompanyId: Guid.NewGuid(),
            CompanyName: "Empresa SIGH LTDA",
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            SocialName: null,
            CpfMasked: "***.***.***-25",
            BirthDate: null,
            AdmissionDate: new DateOnly(2025, 1, 1),
            TerminationDate: null,
            TerminationReason: null,
            Status: Domain.Employees.Enums.EmployeeStatus.PendingAdmission,
            JobTitleId: Guid.NewGuid(),
            JobTitleName: "Desenvolvedor",
            ManagementUnitId: Guid.NewGuid(),
            ManagementUnitName: "Matriz SP",
            DepartmentId: null,
            DepartmentName: null,
            SupervisorId: null,
            SupervisorName: null,
            UserId: null,
            HasSystemAccess: false,
            CorporateEmail: null,
            PersonalEmail: null,
            MobileNumber: null,
            Notes: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null
        );

        _queryServiceMock
            .Setup(q => q.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _useCase.ExecuteAsync(employeeId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FullName.Should().Be("João da Silva");
        result.Data.CpfMasked.Should().Be("***.***.***-25");
        result.Data.CompanyName.Should().Be("Empresa SIGH LTDA");
        result.Data.JobTitleName.Should().Be("Desenvolvedor");
        result.Data.ManagementUnitName.Should().Be("Matriz SP");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_ShouldReturnNotFoundResult()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryServiceMock
            .Setup(q => q.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetEmployeeByIdResponse?)null);

        // Act
        var result = await _useCase.ExecuteAsync(employeeId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.NotFound);
    }
}
