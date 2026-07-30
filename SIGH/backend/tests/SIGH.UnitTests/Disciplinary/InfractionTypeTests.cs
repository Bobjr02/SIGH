using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class InfractionTypeTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateInfractionTypeWithNormalizedCode()
    {
        // Act
        var infractionType = InfractionType.Create(
            code: " inf-001 ",
            name: " Atraso injustificado ",
            defaultSeverity: InfractionSeverity.Low,
            requiresFormalInvestigation: false,
            allowsTerminationRecommendation: false,
            description: "Descrição da infração",
            legalReference: "CLT Art. 482"
        );

        // Assert
        infractionType.Should().NotBeNull();
        infractionType.Code.Should().Be("INF-001");
        infractionType.Name.Should().Be("Atraso injustificado");
        infractionType.DefaultSeverity.Should().Be(InfractionSeverity.Low);
        infractionType.IsActive.Should().BeTrue();
        infractionType.LegalReference.Should().Be("CLT Art. 482");
    }

    [Theory]
    [InlineData(InfractionSeverity.Undefined)]
    [InlineData((InfractionSeverity)999)]
    public void Create_WithInvalidDefaultSeverity_ShouldThrowBusinessRuleValidationException(InfractionSeverity invalidSeverity)
    {
        // Act
        Action act = () => InfractionType.Create(
            code: "INF-001",
            name: "Nome Válido",
            defaultSeverity: invalidSeverity
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A severidade padrão é inválida.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyCode_ShouldThrowBusinessRuleValidationException(string? invalidCode)
    {
        // Act
        Action act = () => InfractionType.Create(
            code: invalidCode!,
            name: "Nome Válido",
            defaultSeverity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O código do tipo de infração é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        // Act
        Action act = () => InfractionType.Create(
            code: "INF-001",
            name: invalidName!,
            defaultSeverity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O nome do tipo de infração é obrigatório.");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var infractionType = InfractionType.Create("INF-001", "Nome Inicial", InfractionSeverity.Low);

        // Act
        infractionType.UpdateDetails("Nome Atualizado", InfractionSeverity.High, true, true, "Nova Descrição", "Nova Ref");

        // Assert
        infractionType.Name.Should().Be("Nome Atualizado");
        infractionType.DefaultSeverity.Should().Be(InfractionSeverity.High);
        infractionType.RequiresFormalInvestigation.Should().BeTrue();
        infractionType.AllowsTerminationRecommendation.Should().BeTrue();
        infractionType.Description.Should().Be("Nova Descrição");
        infractionType.LegalReference.Should().Be("Nova Ref");
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldToggleIsActive()
    {
        // Arrange
        var infractionType = InfractionType.Create("INF-001", "Nome Inicial", InfractionSeverity.Low);

        // Act & Assert
        infractionType.Deactivate();
        infractionType.IsActive.Should().BeFalse();

        infractionType.Activate();
        infractionType.IsActive.Should().BeTrue();
    }
}
