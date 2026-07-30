using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class EmployeeMappingTests
{
    private readonly SighDbContext _context;

    public EmployeeMappingTests()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SighDbContext(options);
    }

    [Fact]
    public void Model_ShouldContainAllEmployeeModuleDbSets()
    {
        _context.Companies.Should().NotBeNull();
        _context.ManagementUnits.Should().NotBeNull();
        _context.Departments.Should().NotBeNull();
        _context.JobTitles.Should().NotBeNull();
        _context.Employees.Should().NotBeNull();
    }

    [Fact]
    public void EntityTypes_ShouldBeRegisteredInModel()
    {
        var model = _context.Model;

        model.FindEntityType(typeof(Company)).Should().NotBeNull();
        model.FindEntityType(typeof(ManagementUnit)).Should().NotBeNull();
        model.FindEntityType(typeof(Department)).Should().NotBeNull();
        model.FindEntityType(typeof(JobTitle)).Should().NotBeNull();
        model.FindEntityType(typeof(Employee)).Should().NotBeNull();
    }

    [Fact]
    public void TableNames_ShouldMatchExpectedConvention()
    {
        var model = _context.Model;

        model.FindEntityType(typeof(Company))!.GetTableName().Should().Be("Companies");
        model.FindEntityType(typeof(ManagementUnit))!.GetTableName().Should().Be("ManagementUnits");
        model.FindEntityType(typeof(Department))!.GetTableName().Should().Be("Departments");
        model.FindEntityType(typeof(JobTitle))!.GetTableName().Should().Be("JobTitles");
        model.FindEntityType(typeof(Employee))!.GetTableName().Should().Be("Employees");
    }

    [Fact]
    public void Employee_HasSystemAccess_ShouldBeIgnoredByEFCore()
    {
        var employeeType = _context.Model.FindEntityType(typeof(Employee))!;
        var property = employeeType.FindProperty(nameof(Employee.HasSystemAccess));

        property.Should().BeNull("HasSystemAccess é uma propriedade derivada/computada no domínio e não deve ser persistida.");
    }

    [Fact]
    public void Employee_Status_ShouldBeMappedAsInteger()
    {
        var employeeType = _context.Model.FindEntityType(typeof(Employee))!;
        var statusProperty = employeeType.FindProperty(nameof(Employee.Status))!;

        statusProperty.ClrType.Should().Be(typeof(EmployeeStatus));
        statusProperty.GetProviderClrType().Should().Be(typeof(int));
    }

    [Fact]
    public void Employee_DateProperties_ShouldBeMappedAsDateOnly()
    {
        var employeeType = _context.Model.FindEntityType(typeof(Employee))!;

        var admissionDateProp = employeeType.FindProperty(nameof(Employee.AdmissionDate))!;
        admissionDateProp.ClrType.Should().Be(typeof(DateOnly));

        var birthDateProp = employeeType.FindProperty(nameof(Employee.BirthDate))!;
        birthDateProp.ClrType.Should().Be(typeof(DateOnly?));

        var terminationDateProp = employeeType.FindProperty(nameof(Employee.TerminationDate))!;
        terminationDateProp.ClrType.Should().Be(typeof(DateOnly?));
    }

    [Fact]
    public void Employee_MaxLengths_ShouldMatchDomainRules()
    {
        var employeeType = _context.Model.FindEntityType(typeof(Employee))!;

        employeeType.FindProperty(nameof(Employee.EmployeeNumber))!.GetMaxLength().Should().Be(20);
        employeeType.FindProperty(nameof(Employee.FullName))!.GetMaxLength().Should().Be(150);
        employeeType.FindProperty(nameof(Employee.SocialName))!.GetMaxLength().Should().Be(150);
        employeeType.FindProperty(nameof(Employee.Cpf))!.GetMaxLength().Should().Be(11);
        employeeType.FindProperty(nameof(Employee.CorporateEmail))!.GetMaxLength().Should().Be(150);
        employeeType.FindProperty(nameof(Employee.PersonalEmail))!.GetMaxLength().Should().Be(150);
        employeeType.FindProperty(nameof(Employee.MobileNumber))!.GetMaxLength().Should().Be(20);
        employeeType.FindProperty(nameof(Employee.TerminationReason))!.GetMaxLength().Should().Be(500);
        employeeType.FindProperty(nameof(Employee.Notes))!.GetMaxLength().Should().Be(1000);
    }

    [Fact]
    public void Relationships_DeleteBehavior_ShouldBeRestrict()
    {
        var employeeType = _context.Model.FindEntityType(typeof(Employee))!;
        var foreignKeys = employeeType.GetForeignKeys();

        foreach (var fk in foreignKeys)
        {
            fk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict, $"Foreign key para {fk.PrincipalEntityType.ClrType.Name} deve ser Restrict para evitar remoção em cascata.");
        }
    }
}
