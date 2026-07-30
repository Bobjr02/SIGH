using FluentAssertions;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class CompanyTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var company = Company.Create("Usina de Açúcar S/A", "Usina de Açúcar Grandes Lagos S/A", "12.345.678/0001-99");

        company.Should().NotBeNull();
        company.Id.Should().NotBe(Guid.Empty);
        company.Name.Should().Be("Usina de Açúcar S/A");
        company.LegalName.Should().Be("Usina de Açúcar Grandes Lagos S/A");
        company.RegistrationNumber.Should().Be("12345678000199");
        company.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        var act = () => Company.Create(invalidName!);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome da empresa é obrigatório.");
    }

    [Fact]
    public void Create_WithNameExceeding150Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longName = new string('A', 151);

        var act = () => Company.Create(longName);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome da empresa deve ter no máximo 150 caracteres.");
    }

    [Fact]
    public void Create_WithLegalNameExceeding200Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longLegalName = new string('B', 201);

        var act = () => Company.Create("Empresa Válida", longLegalName);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("Razão Social deve ter no máximo 200 caracteres.");
    }

    [Fact]
    public void Create_WithRegistrationNumber_ShouldNormalizePunctuationAndWhitespace()
    {
        var company = Company.Create("Empresa S/A", registrationNumber: " 12.345.678 / 0001 - 99 ");

        company.RegistrationNumber.Should().Be("12345678000199");
    }

    [Fact]
    public void Create_WithRegistrationNumberExceeding20Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longRegNum = new string('1', 21);

        var act = () => Company.Create("Empresa S/A", registrationNumber: longRegNum);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O número de registro da empresa deve ter no máximo 20 caracteres.");
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleIsActive()
    {
        var company = Company.Create("Empresa Teste");

        company.Deactivate();
        company.IsActive.Should().BeFalse();

        company.Activate();
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_WithValidAndInvalidData_ShouldValidateCorrectly()
    {
        var company = Company.Create("Nome Inicial");

        company.UpdateName("Nome Atualizado");
        company.Name.Should().Be("Nome Atualizado");

        var act = () => company.UpdateName("");
        act.Should().Throw<BusinessRuleValidationException>();
    }
}
