using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryOccurrenceTests
{
    private readonly Guid _caseId = Guid.NewGuid();
    private readonly Guid _reportedByUserId = Guid.NewGuid();
    private readonly Guid _infractionTypeId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldCreateOccurrence()
    {
        // Act
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now.AddHours(-3),
            reportedAt: _now,
            description: "Atraso injustificado recorrente.",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Low,
            location: "Setor A",
            confidentialityLevel: ConfidentialityLevel.Restricted
        );

        // Assert
        occurrence.Should().NotBeNull();
        occurrence.DisciplinaryCaseId.Should().Be(_caseId);
        occurrence.OccurrenceDate.Should().Be(_now.AddHours(-3));
        occurrence.ReportedAt.Should().Be(_now);
        occurrence.Description.Should().Be("Atraso injustificado recorrente.");
        occurrence.ReportedByUserId.Should().Be(_reportedByUserId);
        occurrence.InfractionTypeId.Should().Be(_infractionTypeId);
        occurrence.Severity.Should().Be(InfractionSeverity.Low);
        occurrence.Location.Should().Be("Setor A");
        occurrence.ConfidentialityLevel.Should().Be(ConfidentialityLevel.Restricted);
        occurrence.Status.Should().Be(OccurrenceStatus.Reported);
    }

    [Fact]
    public void Create_WithDefaultOccurrenceDate_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: default,
            reportedAt: _now,
            description: "Descrição de teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data da ocorrência é inválida.");
    }

    [Fact]
    public void Create_WithDefaultReportedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: default,
            description: "Descrição de teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data do relato é inválida.");
    }

    [Theory]
    [InlineData(InfractionSeverity.Undefined)]
    [InlineData((InfractionSeverity)999)]
    public void Create_WithInvalidSeverity_ShouldThrowBusinessRuleValidationException(InfractionSeverity invalidSeverity)
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Descrição de teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: invalidSeverity
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A severidade da infração é inválida.");
    }

    [Theory]
    [InlineData(ConfidentialityLevel.Undefined)]
    [InlineData((ConfidentialityLevel)999)]
    public void Create_WithInvalidConfidentialityLevel_ShouldThrowBusinessRuleValidationException(ConfidentialityLevel invalidConfidentiality)
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Descrição de teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate,
            confidentialityLevel: invalidConfidentiality
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O nível de confidencialidade é inválido.");
    }

    [Fact]
    public void Create_WithOccurrenceDateAfterReportedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var occurrenceDate = _now.AddHours(2);

        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: occurrenceDate,
            reportedAt: _now,
            description: "Descrição de teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data da ocorrência não pode ser posterior à data do relato.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyDescription_ShouldThrowBusinessRuleValidationException(string? invalidDescription)
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: invalidDescription!,
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A descrição da ocorrência é obrigatória.");
    }

    [Fact]
    public void Create_WithEmptyReportedByUserId_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Descrição válida",
            reportedByUserId: Guid.Empty,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O usuário relator é obrigatório.");
    }

    [Fact]
    public void Create_WithEmptyInfractionTypeId_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Descrição válida",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: Guid.Empty,
            severity: InfractionSeverity.Moderate
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O tipo de infração é obrigatório.");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateDetails()
    {
        // Arrange
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Descrição inicial",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Low
        );

        // Act
        occurrence.UpdateDetails("Descrição alterada", "Novo Local", ConfidentialityLevel.Confidential);

        // Assert
        occurrence.Description.Should().Be("Descrição alterada");
        occurrence.Location.Should().Be("Novo Local");
        occurrence.ConfidentialityLevel.Should().Be(ConfidentialityLevel.Confidential);
    }

    [Fact]
    public void MarkAsValidated_ShouldUpdateStatusToValidated()
    {
        // Arrange
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Ocorrência teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Act
        occurrence.MarkAsValidated();

        // Assert
        occurrence.Status.Should().Be(OccurrenceStatus.Validated);
    }

    [Fact]
    public void MarkAsRejected_ShouldUpdateStatusToRejected()
    {
        // Arrange
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: _caseId,
            occurrenceDate: _now,
            reportedAt: _now,
            description: "Ocorrência teste",
            reportedByUserId: _reportedByUserId,
            infractionTypeId: _infractionTypeId,
            severity: InfractionSeverity.Moderate
        );

        // Act
        occurrence.MarkAsRejected();

        // Assert
        occurrence.Status.Should().Be(OccurrenceStatus.Rejected);
    }
}
