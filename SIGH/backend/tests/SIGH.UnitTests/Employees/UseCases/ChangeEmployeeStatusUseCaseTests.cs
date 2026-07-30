using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.ChangeStatus;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class ChangeEmployeeStatusUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly ChangeEmployeeStatusUseCase _useCase;

    public ChangeEmployeeStatusUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _useCase = new ChangeEmployeeStatusUseCase(_contextMock.Object, _employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ActivateFromPending_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.PendingAdmission);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ChangeEmployeeStatusRequest(employeeId, EmployeeStatus.Active);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.Status.Should().Be(EmployeeStatus.Active);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SetOnLeaveFromActive_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ChangeEmployeeStatusRequest(employeeId, EmployeeStatus.OnLeave);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.Status.Should().Be(EmployeeStatus.OnLeave);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AttemptDirectTerminationViaChangeStatus_ShouldReturnInvalidTransitionFailure()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid(), initialStatus: EmployeeStatus.Active);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ChangeEmployeeStatusRequest(employeeId, EmployeeStatus.Terminated);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidStatusTransition);
    }
}
