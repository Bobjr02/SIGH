using FluentAssertions;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class DepartmentTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var companyId = Guid.NewGuid();
        var mgmtUnitId = Guid.NewGuid();

        var dept = Department.Create(companyId, mgmtUnitId, "Manutenção Mecânica", "MM-01");

        dept.Should().NotBeNull();
        dept.CompanyId.Should().Be(companyId);
        dept.ManagementUnitId.Should().Be(mgmtUnitId);
        dept.Name.Should().Be("Manutenção Mecânica");
        dept.Code.Should().Be("MM-01");
        dept.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyCompanyId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Department.Create(Guid.Empty, Guid.NewGuid(), "Elétrica");

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da empresa é obrigatório.");
    }

    [Fact]
    public void Create_WithEmptyManagementUnitId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Department.Create(Guid.NewGuid(), Guid.Empty, "Elétrica");

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da unidade gestora é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        var act = () => Department.Create(Guid.NewGuid(), Guid.NewGuid(), invalidName!);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome do departamento é obrigatório.");
    }

    [Fact]
    public void Create_WithCodeExceeding30Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longCode = new string('D', 31);

        var act = () => Department.Create(Guid.NewGuid(), Guid.NewGuid(), "Departamento Teste", longCode);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O código do departamento deve ter no máximo 30 caracteres.");
    }

    [Fact]
    public void ChangeManagementUnit_WithEmptyGuid_ShouldThrowBusinessRuleValidationException()
    {
        var dept = Department.Create(Guid.NewGuid(), Guid.NewGuid(), "Departamento Teste");

        var act = () => dept.ChangeManagementUnit(Guid.Empty);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da unidade gestora é obrigatório.");
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleIsActive()
    {
        var dept = Department.Create(Guid.NewGuid(), Guid.NewGuid(), "Departamento Teste");

        dept.Deactivate();
        dept.IsActive.Should().BeFalse();

        dept.Activate();
        dept.IsActive.Should().BeTrue();
    }
}
