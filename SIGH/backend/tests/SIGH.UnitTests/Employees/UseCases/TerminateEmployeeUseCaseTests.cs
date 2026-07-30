using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.TerminateEmployee;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class TerminateEmployeeUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly TerminateEmployeeUseCase _useCase;

    public TerminateEmployeeUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _useCase = new TerminateEmployeeUseCase(_contextMock.Object, _employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTermination_ShouldTerminateEmployeeAndReturnResponse()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 15), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new TerminateEmployeeRequest(employeeId, new DateOnly(2025, 6, 30), "Desligamento a pedido");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(EmployeeStatus.Terminated);
        result.Data.TerminationDate.Should().Be(new DateOnly(2025, 6, 30));
        result.Data.TerminationReason.Should().Be("Desligamento a pedido");

        employee.Status.Should().Be(EmployeeStatus.Terminated);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTerminationDateBeforeAdmission_ShouldReturnFailure()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 15), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new TerminateEmployeeRequest(employeeId, new DateOnly(2024, 12, 31), "Data invalida");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }
}
