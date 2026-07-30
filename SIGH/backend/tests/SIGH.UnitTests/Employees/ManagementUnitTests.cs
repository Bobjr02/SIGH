using FluentAssertions;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class ManagementUnitTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var companyId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var unit = ManagementUnit.Create(companyId, "Gerência Industrial", "GI-01", parentId);

        unit.Should().NotBeNull();
        unit.CompanyId.Should().Be(companyId);
        unit.Name.Should().Be("Gerência Industrial");
        unit.Code.Should().Be("GI-01");
        unit.ParentManagementUnitId.Should().Be(parentId);
        unit.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyCompanyId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => ManagementUnit.Create(Guid.Empty, "Gerência de Manutenção");

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da empresa é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        var act = () => ManagementUnit.Create(Guid.NewGuid(), invalidName!);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome da unidade gestora é obrigatório.");
    }

    [Fact]
    public void Create_WithCodeExceeding30Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longCode = new string('C', 31);

        var act = () => ManagementUnit.Create(Guid.NewGuid(), "Gerência Agrícola", code: longCode);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O código da unidade gestora deve ter no máximo 30 caracteres.");
    }

    [Fact]
    public void ChangeParent_WithSelfId_ShouldThrowBusinessRuleValidationException()
    {
        var unitId = Guid.NewGuid();
        var unit = new ManagementUnit(unitId);

        var act = () => unit.ChangeParent(unitId);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("Uma unidade gestora não pode ser pai de si mesma.");
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleIsActive()
    {
        var unit = ManagementUnit.Create(Guid.NewGuid(), "Diretoria");

        unit.Deactivate();
        unit.IsActive.Should().BeFalse();

        unit.Activate();
        unit.IsActive.Should().BeTrue();
    }
}
