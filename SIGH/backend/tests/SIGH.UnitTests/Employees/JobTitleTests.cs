using FluentAssertions;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class JobTitleTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var companyId = Guid.NewGuid();

        var jobTitle = JobTitle.Create(companyId, "Engenheiro de Manutenção", "ENG-01", "Responsável pelas rotinas de manutenção industrial.");

        jobTitle.Should().NotBeNull();
        jobTitle.CompanyId.Should().Be(companyId);
        jobTitle.Name.Should().Be("Engenheiro de Manutenção");
        jobTitle.Code.Should().Be("ENG-01");
        jobTitle.Description.Should().Be("Responsável pelas rotinas de manutenção industrial.");
        jobTitle.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyCompanyId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => JobTitle.Create(Guid.Empty, "Operador de Caldeira");

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da empresa é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        var act = () => JobTitle.Create(Guid.NewGuid(), invalidName!);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome do cargo é obrigatório.");
    }

    [Fact]
    public void Create_WithDescriptionExceeding500Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longDesc = new string('J', 501);

        var act = () => JobTitle.Create(Guid.NewGuid(), "Cargo Teste", description: longDesc);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A descrição do cargo deve ter no máximo 500 caracteres.");
    }

    [Fact]
    public void UpdateNameAndDescription_ShouldUpdateCorrectly()
    {
        var jobTitle = JobTitle.Create(Guid.NewGuid(), "Mecânico I");

        jobTitle.UpdateName("Mecânico II");
        jobTitle.UpdateDescription("Atividades de maior complexidade.");

        jobTitle.Name.Should().Be("Mecânico II");
        jobTitle.Description.Should().Be("Atividades de maior complexidade.");
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleIsActive()
    {
        var jobTitle = JobTitle.Create(Guid.NewGuid(), "Eletricista");

        jobTitle.Deactivate();
        jobTitle.IsActive.Should().BeFalse();

        jobTitle.Activate();
        jobTitle.IsActive.Should().BeTrue();
    }
}
