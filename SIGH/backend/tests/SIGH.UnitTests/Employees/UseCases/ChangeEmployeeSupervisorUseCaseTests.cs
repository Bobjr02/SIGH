using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.ChangeSupervisor;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class ChangeEmployeeSupervisorUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly ChangeEmployeeSupervisorUseCase _useCase;

    public ChangeEmployeeSupervisorUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _useCase = new ChangeEmployeeSupervisorUseCase(_contextMock.Object, _employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidSupervisorInSameCompany_ShouldAssignSupervisor()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var subordinateId = Guid.NewGuid();

        var supervisor = Employee.Create(companyId, "EMP-001", "Gestor Santos", "52998224725", new DateOnly(2020, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(supervisor, supervisorId);

        var subordinate = Employee.Create(companyId, "EMP-002", "Subordinado Lima", "11122233344", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(subordinate, subordinateId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(subordinateId, It.IsAny<CancellationToken>())).ReturnsAsync(subordinate);
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(supervisorId, It.IsAny<CancellationToken>())).ReturnsAsync(supervisor);

        var request = new ChangeEmployeeSupervisorRequest(subordinateId, supervisorId);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        subordinate.SupervisorId.Should().Be(supervisorId);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSupervisorFromDifferentCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId1 = Guid.NewGuid();
        var companyId2 = Guid.NewGuid();
        var emp1Id = Guid.NewGuid();
        var emp2Id = Guid.NewGuid();

        var emp1 = Employee.Create(companyId1, "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(emp1, emp1Id);

        var emp2 = Employee.Create(companyId2, "EMP-002", "Func 2", "11122233344", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(emp2, emp2Id);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(emp1Id, It.IsAny<CancellationToken>())).ReturnsAsync(emp1);
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(emp2Id, It.IsAny<CancellationToken>())).ReturnsAsync(emp2);

        var request = new ChangeEmployeeSupervisorRequest(emp1Id, emp2Id);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.SupervisorFromAnotherCompany);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSelfSupervisionAttempted_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var employee = Employee.Create(companyId, "EMP-001", "Func 1", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ChangeEmployeeSupervisorRequest(employeeId, employeeId);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }
}
