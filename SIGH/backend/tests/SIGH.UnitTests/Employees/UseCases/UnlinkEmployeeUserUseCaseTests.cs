using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.UnlinkUser;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class UnlinkEmployeeUserUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly UnlinkEmployeeUserUseCase _useCase;

    public UnlinkEmployeeUserUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _useCase = new UnlinkEmployeeUserUseCase(_contextMock.Object, _employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserLinked_ShouldUnlinkUserAndReturnSuccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2020, 1, 15), Guid.NewGuid(), Guid.NewGuid(), userId: userId);
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new UnlinkEmployeeUserRequest(employeeId);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.UserId.Should().BeNull();
        employee.HasSystemAccess.Should().BeFalse();

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
