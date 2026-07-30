using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.LinkUser;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class LinkEmployeeUserUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly LinkEmployeeUserUseCase _useCase;

    public LinkEmployeeUserUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _useCase = new LinkEmployeeUserUseCase(
            _contextMock.Object,
            _employeeRepositoryMock.Object,
            _userRepositoryMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserNotLinked_ShouldLinkUser()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2020, 1, 15), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _userRepositoryMock.Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _employeeRepositoryMock.Setup(r => r.IsUserLinkedAsync(userId, employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new LinkEmployeeUserRequest(employeeId, userId);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.UserId.Should().Be(userId);
        employee.HasSystemAccess.Should().BeTrue();

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var employee = Employee.Create(Guid.NewGuid(), "EMP-001", "Func 1", "52998224725", new DateOnly(2020, 1, 15), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _userRepositoryMock.Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new LinkEmployeeUserRequest(employeeId, userId);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.UserNotFound);
    }
}
