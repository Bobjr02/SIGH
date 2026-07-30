using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.ChangeOrganizationalAssignment;
using SIGH.Application.Employees.Common;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class ChangeEmployeeOrganizationalAssignmentUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IJobTitleRepository> _jobTitleRepositoryMock;
    private readonly Mock<IManagementUnitRepository> _managementUnitRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly ChangeEmployeeOrganizationalAssignmentUseCase _useCase;

    public ChangeEmployeeOrganizationalAssignmentUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _jobTitleRepositoryMock = new Mock<IJobTitleRepository>();
        _managementUnitRepositoryMock = new Mock<IManagementUnitRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();

        _useCase = new ChangeEmployeeOrganizationalAssignmentUseCase(
            _contextMock.Object,
            _employeeRepositoryMock.Object,
            _jobTitleRepositoryMock.Object,
            _managementUnitRepositoryMock.Object,
            _departmentRepositoryMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithValidNewAssignment_ShouldUpdateAssignmentAndReturnSuccess()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId2 = Guid.NewGuid();
        var jobTitleId2 = Guid.NewGuid();
        var departmentId2 = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var unit2 = ManagementUnit.Create(companyId, "Unidade 2");
        typeof(ManagementUnit).GetProperty(nameof(ManagementUnit.Id))!.SetValue(unit2, unitId2);

        var job2 = JobTitle.Create(companyId, "Dev Senior");
        typeof(JobTitle).GetProperty(nameof(JobTitle.Id))!.SetValue(job2, jobTitleId2);

        var dept2 = Department.Create(companyId, unitId2, "Tecnologia");
        typeof(Department).GetProperty(nameof(Department.Id))!.SetValue(dept2, departmentId2);

        var employee = Employee.Create(companyId, "EMP-001", "Ana Souza", "52998224725", new DateOnly(2025, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId2, It.IsAny<CancellationToken>())).ReturnsAsync(job2);
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId2, It.IsAny<CancellationToken>())).ReturnsAsync(unit2);
        _departmentRepositoryMock.Setup(r => r.GetByIdAsync(departmentId2, It.IsAny<CancellationToken>())).ReturnsAsync(dept2);

        var request = new ChangeEmployeeOrganizationalAssignmentRequest(employeeId, jobTitleId2, unitId2, departmentId2);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        employee.JobTitleId.Should().Be(jobTitleId2);
        employee.ManagementUnitId.Should().Be(unitId2);
        employee.DepartmentId.Should().Be(departmentId2);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmployeeNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var request = new ChangeEmployeeOrganizationalAssignmentRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(request.EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.NotFound);
    }
}
