using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.UpdateEmployee;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class UpdateEmployeeUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ICompanyRepository> _companyRepositoryMock;
    private readonly Mock<IManagementUnitRepository> _managementUnitRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IJobTitleRepository> _jobTitleRepositoryMock;
    private readonly UpdateEmployeeUseCase _useCase;

    public UpdateEmployeeUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _companyRepositoryMock = new Mock<ICompanyRepository>();
        _managementUnitRepositoryMock = new Mock<IManagementUnitRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _jobTitleRepositoryMock = new Mock<IJobTitleRepository>();

        _useCase = new UpdateEmployeeUseCase(
            _contextMock.Object,
            _employeeRepositoryMock.Object,
            _companyRepositoryMock.Object,
            _managementUnitRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _jobTitleRepositoryMock.Object
        );
    }

    private (Employee employee, Company company, ManagementUnit unit, JobTitle jobTitle) SetupEmployeeAndPrerequisites(Guid companyId, Guid unitId, Guid jobTitleId, Guid employeeId)
    {
        var company = Company.Create("Empresa SIGH", "Empresa SIGH LTDA", "12345678000195");
        typeof(Company).GetProperty(nameof(Company.Id))!.SetValue(company, companyId);

        var unit = ManagementUnit.Create(companyId, "Matriz SP", "UG-01");
        typeof(ManagementUnit).GetProperty(nameof(ManagementUnit.Id))!.SetValue(unit, unitId);

        var jobTitle = JobTitle.Create(companyId, "Desenvolvedor", "DEV");
        typeof(JobTitle).GetProperty(nameof(JobTitle.Id))!.SetValue(jobTitle, jobTitleId);

        var employee = Employee.Create(
            companyId: companyId,
            employeeNumber: "EMP-001",
            fullName: "João da Silva",
            cpf: "52998224725",
            admissionDate: new DateOnly(2025, 1, 1),
            jobTitleId: jobTitleId,
            managementUnitId: unitId
        );
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);

        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _companyRepositoryMock.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId, It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>())).ReturnsAsync(jobTitle);

        return (employee, company, unit, jobTitle);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldUpdateEmployeeAndReturnOkResultWithMaskedCpf()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-002",
            FullName: "João da Silva Sauro",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FullName.Should().Be("João da Silva Sauro");
        result.Data.CpfMasked.Should().Be("***.***.***-25");

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenJobTitleFromAnotherCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        var jobTitleOther = JobTitle.Create(otherCompanyId, "Cargo Outra Empresa", "DEV2");
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>())).ReturnsAsync(jobTitleOther);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-002",
            FullName: "João da Silva Sauro",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }

    [Fact]
    public async Task ExecuteAsync_WhenManagementUnitFromAnotherCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        var unitOther = ManagementUnit.Create(otherCompanyId, "Unidade Outra Empresa", "UG-99");
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId, It.IsAny<CancellationToken>())).ReturnsAsync(unitOther);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-002",
            FullName: "João da Silva Sauro",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDepartmentFromAnotherManagementUnit_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var otherUnitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        var departmentOther = Department.Create(companyId, otherUnitId, "Depto Outra UG", "DEP-01");
        _departmentRepositoryMock.Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>())).ReturnsAsync(departmentOther);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-002",
            FullName: "João da Silva Sauro",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            DepartmentId: departmentId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InvalidData);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDuplicateCpfExcludingSelf_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByCpfAsync("11122233344", employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "11122233344",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.CpfAlreadyExists);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDuplicateEmployeeNumberExcludingSelf_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmployeeNumberAsync(companyId, "EMP-999", employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-999",
            FullName: "João da Silva",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.EmployeeNumberAlreadyExists);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDuplicateCorporateEmailExcludingSelf_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        SetupEmployeeAndPrerequisites(companyId, unitId, jobTitleId, employeeId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByCorporateEmailAsync("outro@sigh.com", employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateEmployeeRequest(
            EmployeeId: employeeId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            BirthDate: new DateOnly(1990, 5, 20),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            CorporateEmail: "outro@sigh.com"
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.CorporateEmailAlreadyExists);
    }
}
