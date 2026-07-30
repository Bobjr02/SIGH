using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.CreateEmployee;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees.UseCases;

public class CreateEmployeeUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ICompanyRepository> _companyRepositoryMock;
    private readonly Mock<IManagementUnitRepository> _managementUnitRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IJobTitleRepository> _jobTitleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly CreateEmployeeUseCase _useCase;

    public CreateEmployeeUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _companyRepositoryMock = new Mock<ICompanyRepository>();
        _managementUnitRepositoryMock = new Mock<IManagementUnitRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _jobTitleRepositoryMock = new Mock<IJobTitleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _useCase = new CreateEmployeeUseCase(
            _contextMock.Object,
            _employeeRepositoryMock.Object,
            _companyRepositoryMock.Object,
            _managementUnitRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _jobTitleRepositoryMock.Object,
            _userRepositoryMock.Object
        );
    }

    private (Company company, ManagementUnit unit, JobTitle jobTitle) SetupValidPrerequisites(Guid companyId, Guid unitId, Guid jobTitleId, bool active = true)
    {
        var company = Company.Create("Empresa SIGH", "Empresa SIGH LTDA", "12345678000195");
        typeof(Company).GetProperty(nameof(Company.Id))!.SetValue(company, companyId);
        if (!active) company.Deactivate();

        var unit = ManagementUnit.Create(companyId, "Matriz SP", "UG-01");
        typeof(ManagementUnit).GetProperty(nameof(ManagementUnit.Id))!.SetValue(unit, unitId);
        if (!active) unit.Deactivate();

        var jobTitle = JobTitle.Create(companyId, "Desenvolvedor", "DEV");
        typeof(JobTitle).GetProperty(nameof(JobTitle.Id))!.SetValue(jobTitle, jobTitleId);
        if (!active) jobTitle.Deactivate();

        _companyRepositoryMock.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId, It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>())).ReturnsAsync(jobTitle);

        return (company, unit, jobTitle);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldCreateEmployeeAndReturnOkResultWithMaskedCpf()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        SetupValidPrerequisites(companyId, unitId, jobTitleId);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FullName.Should().Be("João da Silva");
        result.Data.CpfMasked.Should().Be("***.***.***-25");
        result.Data.Status.Should().Be(EmployeeStatus.PendingAdmission);

        _employeeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompanyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateEmployeeRequest(
            CompanyId: Guid.NewGuid(),
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: Guid.NewGuid(),
            ManagementUnitId: Guid.NewGuid()
        );

        _companyRepositoryMock.Setup(r => r.GetByIdAsync(request.CompanyId, It.IsAny<CancellationToken>())).ReturnsAsync((Company?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.CompanyNotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompanyInactive_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        SetupValidPrerequisites(companyId, unitId, jobTitleId, active: false);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InactiveCompany);
    }

    [Fact]
    public async Task ExecuteAsync_WhenJobTitleFromAnotherCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var jobTitle = JobTitle.Create(otherCompanyId, "Cargo Outra Empresa", "DEV2");
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>())).ReturnsAsync(jobTitle);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
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
    public async Task ExecuteAsync_WhenJobTitleInactive_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var jobTitle = JobTitle.Create(companyId, "Cargo Inativo", "DEV2");
        jobTitle.Deactivate();
        _jobTitleRepositoryMock.Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>())).ReturnsAsync(jobTitle);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InactiveJobTitle);
    }

    [Fact]
    public async Task ExecuteAsync_WhenManagementUnitFromAnotherCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var unit = ManagementUnit.Create(otherCompanyId, "Unidade Outra Empresa", "UG-99");
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId, It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
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
    public async Task ExecuteAsync_WhenManagementUnitInactive_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var unit = ManagementUnit.Create(companyId, "Unidade Inativa", "UG-99");
        unit.Deactivate();
        _managementUnitRepositoryMock.Setup(r => r.GetByIdAsync(unitId, It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InactiveManagementUnit);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDepartmentFromAnotherManagementUnit_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var otherUnitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var department = Department.Create(companyId, otherUnitId, "Depto Outra UG", "DEP-01");
        _departmentRepositoryMock.Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>())).ReturnsAsync(department);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
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
    public async Task ExecuteAsync_WhenDepartmentInactive_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        var department = Department.Create(companyId, unitId, "Depto Inativo", "DEP-01");
        department.Deactivate();
        _departmentRepositoryMock.Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>())).ReturnsAsync(department);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            DepartmentId: departmentId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.InactiveDepartment);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSupervisorFromAnotherCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);

        var supervisor = Employee.Create(
            companyId: otherCompanyId,
            employeeNumber: "SUP-001",
            fullName: "Gestor Outra Empresa",
            cpf: "11122233344",
            admissionDate: new DateOnly(2020, 1, 1),
            jobTitleId: jobTitleId,
            managementUnitId: unitId
        );
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(supervisorId, It.IsAny<CancellationToken>())).ReturnsAsync(supervisor);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            SupervisorId: supervisorId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.SupervisorFromAnotherCompany);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDuplicateCpf_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        SetupValidPrerequisites(companyId, unitId, jobTitleId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByCpfAsync("52998224725", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
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
    public async Task ExecuteAsync_WhenDuplicateEmployeeNumberInSameCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        SetupValidPrerequisites(companyId, unitId, jobTitleId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByEmployeeNumberAsync(companyId, "EMP-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
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
    public async Task ExecuteAsync_WhenDuplicateCorporateEmail_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        SetupValidPrerequisites(companyId, unitId, jobTitleId);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByCorporateEmailAsync("joao@sigh.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            CorporateEmail: "joao@sigh.com"
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.CorporateEmailAlreadyExists);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyLinkedToAnotherEmployee_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        SetupValidPrerequisites(companyId, unitId, jobTitleId);
        _userRepositoryMock.Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _employeeRepositoryMock.Setup(r => r.IsUserLinkedAsync(userId, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new CreateEmployeeRequest(
            CompanyId: companyId,
            EmployeeNumber: "EMP-001",
            FullName: "João da Silva",
            Cpf: "52998224725",
            AdmissionDate: new DateOnly(2025, 1, 1),
            JobTitleId: jobTitleId,
            ManagementUnitId: unitId,
            UserId: userId
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(EmployeeErrors.UserAlreadyLinked);
    }
}
