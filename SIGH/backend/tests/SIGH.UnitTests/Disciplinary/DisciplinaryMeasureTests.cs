using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryMeasureTests
{
    private readonly Guid _caseId = Guid.NewGuid();
    private readonly Guid _decisionId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _appliedByUserId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WrittenWarningWithoutEffectiveUntil_ShouldCreatePendingMeasureWithoutAppliedAt()
    {
        // Act
        var measure = DisciplinaryMeasure.Create(
            disciplinaryCaseId: _caseId,
            disciplinaryDecisionId: _decisionId,
            employeeId: _employeeId,
            measureType: DisciplinaryMeasureType.WrittenWarning,
            reason: "Falta injustificada reiterada",
            effectiveFrom: _now
        );

        // Assert
        measure.Should().NotBeNull();
        measure.DisciplinaryCaseId.Should().Be(_caseId);
        measure.DisciplinaryDecisionId.Should().Be(_decisionId);
        measure.EmployeeId.Should().Be(_employeeId);
        measure.MeasureType.Should().Be(DisciplinaryMeasureType.WrittenWarning);
        measure.Reason.Should().Be("Falta injustificada reiterada");
        measure.AppliedAt.Should().BeNull();
        measure.AppliedByUserId.Should().BeNull();
        measure.EffectiveFrom.Should().Be(_now);
        measure.EffectiveUntil.Should().BeNull();
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Pending);
    }

    [Fact]
    public void Create_SuspensionWithValidPeriod_ShouldCreateMeasure()
    {
        // Arrange
        var effectiveFrom = _now;
        var effectiveUntil = _now.AddDays(3);

        // Act
        var measure = DisciplinaryMeasure.Create(
            disciplinaryCaseId: _caseId,
            disciplinaryDecisionId: _decisionId,
            employeeId: _employeeId,
            measureType: DisciplinaryMeasureType.Suspension,
            reason: "Suspensão disciplinar por 3 dias",
            effectiveFrom: effectiveFrom,
            effectiveUntil: effectiveUntil
        );

        // Assert
        measure.MeasureType.Should().Be(DisciplinaryMeasureType.Suspension);
        measure.EffectiveUntil.Should().Be(effectiveUntil);
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Pending);
    }

    [Fact]
    public void Create_WithDefaultEffectiveFrom_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryMeasure.Create(
            disciplinaryCaseId: _caseId,
            disciplinaryDecisionId: _decisionId,
            employeeId: _employeeId,
            measureType: DisciplinaryMeasureType.WrittenWarning,
            reason: "Motivo válido",
            effectiveFrom: default
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de início da vigência é inválida.");
    }

    [Theory]
    [InlineData(DisciplinaryMeasureType.Undefined)]
    [InlineData((DisciplinaryMeasureType)999)]
    public void Create_WithInvalidMeasureType_ShouldThrowBusinessRuleValidationException(DisciplinaryMeasureType invalidType)
    {
        // Act
        Action act = () => DisciplinaryMeasure.Create(
            disciplinaryCaseId: _caseId,
            disciplinaryDecisionId: _decisionId,
            employeeId: _employeeId,
            measureType: invalidType,
            reason: "Motivo válido",
            effectiveFrom: _now
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O tipo de medida disciplinar é inválido.");
    }

    [Fact]
    public void Apply_PendingMeasure_ShouldSetAppliedAtAndUserIdAndTransitionToApplied()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);

        // Act
        measure.Apply(_now.AddHours(1), _appliedByUserId);

        // Assert
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Applied);
        measure.AppliedAt.Should().Be(_now.AddHours(1));
        measure.AppliedByUserId.Should().Be(_appliedByUserId);
    }

    [Fact]
    public void Apply_AlreadyAppliedMeasure_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);
        measure.Apply(_now.AddHours(1), _appliedByUserId);

        // Act - Attempting to apply a second time
        Action act = () => measure.Apply(_now.AddHours(2), _appliedByUserId);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas medidas pendentes podem ser aplicadas.");
    }

    [Fact]
    public void Complete_PendingMeasureDirectly_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);

        // Act - Attempting to complete before applying
        Action act = () => measure.Complete();

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas medidas aplicadas podem ser concluídas.");
    }

    [Fact]
    public void Complete_AppliedMeasure_ShouldTransitionToCompleted()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);
        measure.Apply(_now.AddHours(1), _appliedByUserId);

        // Act
        measure.Complete();

        // Assert
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Completed);
    }

    [Fact]
    public void Cancel_PendingMeasure_ShouldTransitionToCancelled()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);

        // Act
        measure.Cancel("Revogada pelo RH");

        // Assert
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AppliedMeasure_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);
        measure.Apply(_now.AddHours(1), _appliedByUserId);

        // Act
        Action act = () => measure.Cancel("Cancelamento");

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas medidas disciplinares no estado Pendente podem ser canceladas.");
    }

    [Fact]
    public void Cancel_CompletedMeasure_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var measure = DisciplinaryMeasure.Create(_caseId, _decisionId, _employeeId, DisciplinaryMeasureType.WrittenWarning, "Motivo", _now);
        measure.Apply(_now.AddHours(1), _appliedByUserId);
        measure.Complete();

        // Act
        Action act = () => measure.Cancel("Cancelamento");

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas medidas disciplinares no estado Pendente podem ser canceladas.");
    }
}
