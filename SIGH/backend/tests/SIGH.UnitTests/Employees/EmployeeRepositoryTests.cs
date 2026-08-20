using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Persistence.Context;
using SIGH.Persistence.Repositories;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class EmployeeRepositoryTests
{
    private readonly SighDbContext _context;
    private readonly EmployeeRepository _repository;

    public EmployeeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SighDbContext(options);
        _repository = new EmployeeRepository(_context);
    }

    private async Task<(Company company, ManagementUnit unit, JobTitle jobTitle)> CreatePrerequisitesAsync()
    {
        var company = Company.Create("Empresa Teste", "Empresa Teste LTDA", "12345678000195");
        await _context.Companies.AddAsync(company);

        var unit = ManagementUnit.Create(company.Id, "Unidade Matriz", "UG-01");
        await _context.ManagementUnits.AddAsync(unit);

        var jobTitle = JobTitle.Create(company.Id, "Desenvolvedor Sênior", "DEV-SR");
        await _context.JobTitles.AddAsync(jobTitle);

        await _context.SaveChangesAsync();
        return (company, unit, jobTitle);
    }

    private Employee CreateSampleEmployee(Company company, ManagementUnit unit, JobTitle jobTitle, string cpf = "52998224725", string number = "EMP-001", string? corporateEmail = null)
    {
        return Employee.Create(
            companyId: company.Id,
            employeeNumber: number,
            fullName: "Carlos Eduardo da Silva",
            cpf: cpf,
            admissionDate: new DateOnly(2025, 1, 15),
            jobTitleId: jobTitle.Id,
            managementUnitId: unit.Id,
            corporateEmail: corporateEmail
        );
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveEmployee()
    {
        // Arrange
        var (company, unit, jobTitle) = await CreatePrerequisitesAsync();
        var employee = CreateSampleEmployee(company, unit, jobTitle);

        // Act
        await _repository.AddAsync(employee);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(employee.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(employee.Id);
        result.FullName.Should().Be("Carlos Eduardo da Silva");
        result.Cpf.Should().Be("52998224725");
        result.Status.Should().Be(EmployeeStatus.PendingAdmission);
    }

    [Fact]
    public async Task ExistsByCpfAsync_ShouldDetectExistingCpf_And_IgnoreWhenExcludingSelf()
    {
        // Arrange
        var (company, unit, jobTitle) = await CreatePrerequisitesAsync();
        var employee = CreateSampleEmployee(company, unit, jobTitle, cpf: "52998224725");
        await _repository.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Act
        var existsFormatted = await _repository.ExistsByCpfAsync("529.982.247-25");
        var existsSelfExcluded = await _repository.ExistsByCpfAsync("529.982.247-25", excludingEmployeeId: employee.Id);
        var existsOtherCpf = await _repository.ExistsByCpfAsync("11122233344");

        // Assert
        existsFormatted.Should().BeTrue();
        existsSelfExcluded.Should().BeFalse();
        existsOtherCpf.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByEmployeeNumberAsync_ShouldScopeByCompany()
    {
        // Arrange
        var (company1, unit1, jobTitle1) = await CreatePrerequisitesAsync();
        var company2 = Company.Create("Outra Empresa");
        await _context.Companies.AddAsync(company2);
        var unit2 = ManagementUnit.Create(company2.Id, "Unidade Filial");
        await _context.ManagementUnits.AddAsync(unit2);
        var jobTitle2 = JobTitle.Create(company2.Id, "Analista");
        await _context.JobTitles.AddAsync(jobTitle2);
        await _context.SaveChangesAsync();

        var emp1 = CreateSampleEmployee(company1, unit1, jobTitle1, cpf: "52998224725", number: "EMP-100");
        await _repository.AddAsync(emp1);
        await _context.SaveChangesAsync();

        // Act
        var existsInCompany1 = await _repository.ExistsByEmployeeNumberAsync(company1.Id, "EMP-100");
        var existsInCompany2 = await _repository.ExistsByEmployeeNumberAsync(company2.Id, "EMP-100");
        var existsExcludedSelf = await _repository.ExistsByEmployeeNumberAsync(company1.Id, "EMP-100", excludingEmployeeId: emp1.Id);

        // Assert
        existsInCompany1.Should().BeTrue();
        existsInCompany2.Should().BeFalse("A mesma matrícula em outra empresa é permitida.");
        existsExcludedSelf.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByCorporateEmailAsync_ShouldDetectEmail()
    {
        // Arrange
        var (company, unit, jobTitle) = await CreatePrerequisitesAsync();
        var employee = CreateSampleEmployee(company, unit, jobTitle, corporateEmail: "carlos.silva@empresa.com.br");
        await _repository.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsByCorporateEmailAsync("carlos.silva@empresa.com.br");
        var existsSelfExcluded = await _repository.ExistsByCorporateEmailAsync("carlos.silva@empresa.com.br", excludingEmployeeId: employee.Id);
        var existsOther = await _repository.ExistsByCorporateEmailAsync("outro@empresa.com.br");

        // Assert
        exists.Should().BeTrue();
        existsSelfExcluded.Should().BeFalse();
        existsOther.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserLinkedAsync_ShouldDetectLinkedUser()
    {
        // Arrange
        var (company, unit, jobTitle) = await CreatePrerequisitesAsync();
        var userId = Guid.NewGuid();
        var employee = CreateSampleEmployee(company, unit, jobTitle);
        employee.LinkUser(userId);
        await _repository.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Act
        var isLinked = await _repository.IsUserLinkedAsync(userId);
        var isLinkedSelfExcluded = await _repository.IsUserLinkedAsync(userId, excludingEmployeeId: employee.Id);
        var isOtherLinked = await _repository.IsUserLinkedAsync(Guid.NewGuid());

        // Assert
        isLinked.Should().BeTrue();
        isLinkedSelfExcluded.Should().BeFalse();
        isOtherLinked.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeletedEmployee_ShouldBeHiddenFromStandardQueries()
    {
        // Arrange
        var (company, unit, jobTitle) = await CreatePrerequisitesAsync();
        var employee = CreateSampleEmployee(company, unit, jobTitle);
        await _repository.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Soft delete via DbContext
        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        // Act
        var queriedNormal = await _repository.GetByIdAsync(employee.Id);
        var queriedWithIgnoreFilter = await _context.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == employee.Id);

        // Assert
        queriedNormal.Should().BeNull("Global Query Filter deve ocultar registro com IsDeleted = true.");
        queriedWithIgnoreFilter.Should().NotBeNull();
        queriedWithIgnoreFilter!.IsDeleted.Should().BeTrue();
    }
}
