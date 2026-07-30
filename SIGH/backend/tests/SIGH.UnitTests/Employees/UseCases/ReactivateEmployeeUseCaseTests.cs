using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.ReactivateEmployee;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class ReactivateEmployeeUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly ReactivateEmployeeUseCase _useCase;

    public ReactivateEmployeeUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _useCase = new ReactivateEmployeeUseCase(_contextMock.Object, _employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmployeeIsTerminated_ShouldReactivateAndReturnSuccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2020, 1, 15), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        employee.Terminate(new DateOnly(2024, 1, 1), "Desligado");
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ReactivateEmployeeRequest(employeeId, new DateOnly(2025, 1, 1));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.AdmissionDate.Should().Be(new DateOnly(2025, 1, 1));
        employee.TerminationDate.Should().BeNull();
        employee.TerminationReason.Should().BeNull();

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmployeeIsNotTerminated_ShouldReturnFailure()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2020, 1, 15), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ReactivateEmployeeRequest(employeeId, new DateOnly(2025, 1, 1));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }
}
