using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryDecisionTests
{
    private readonly Guid _caseId = Guid.NewGuid();
    private readonly Guid _decidedByUserId = Guid.NewGuid();
    private readonly Guid _approvedByUserId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldCreateDecisionInDraftStatusByDefault()
    {
        // Act
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: _caseId,
            decisionType: DecisionType.FormalWarning,
            summary: "Advertência escrita recomendada.",
            reasoning: "Reincidência em descumprimento de prazos e políticas.",
            decidedAt: _now,
            decidedByUserId: _decidedByUserId
        );

        // Assert
        decision.Should().NotBeNull();
        decision.DisciplinaryCaseId.Should().Be(_caseId);
        decision.DecisionType.Should().Be(DecisionType.FormalWarning);
        decision.Summary.Should().Be("Advertência escrita recomendada.");
        decision.Reasoning.Should().Be("Reincidência em descumprimento de prazos e políticas.");
        decision.DecidedAt.Should().Be(_now);
        decision.DecidedByUserId.Should().Be(_decidedByUserId);
        decision.Status.Should().Be(DecisionStatus.Draft);
    }

    [Fact]
    public void Create_WithDefaultDecidedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryDecision.Create(
            disciplinaryCaseId: _caseId,
            decisionType: DecisionType.FormalWarning,
            summary: "Resumo válido",
            reasoning: "Fundamentação válida",
            decidedAt: default,
            decidedByUserId: _decidedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data da decisão é inválida.");
    }

    [Theory]
    [InlineData(DecisionType.Undefined)]
    [InlineData((DecisionType)999)]
    public void Create_WithInvalidDecisionType_ShouldThrowBusinessRuleValidationException(DecisionType invalidType)
    {
        // Act
        Action act = () => DisciplinaryDecision.Create(
            disciplinaryCaseId: _caseId,
            decisionType: invalidType,
            summary: "Resumo válido",
            reasoning: "Fundamentação válida",
            decidedAt: _now,
            decidedByUserId: _decidedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O tipo de decisão é inválido.");
    }

    [Fact]
    public void RequiresApproval_ShouldReturnFalseForNoViolationAndTrueForOthers()
    {
        // Arrange
        var noViolationDecision = DisciplinaryDecision.Create(_caseId, DecisionType.NoViolation, "Resumo", "Motivo", _now, _decidedByUserId);
        var warningDecision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);

        // Act & Assert
        noViolationDecision.RequiresApproval().Should().BeFalse();
        warningDecision.RequiresApproval().Should().BeTrue();
    }

    [Fact]
    public void SubmitForApproval_FromDraft_ShouldTransitionToPendingApproval()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);

        // Act
        decision.SubmitForApproval();

        // Assert
        decision.Status.Should().Be(DecisionStatus.PendingApproval);
    }

    [Fact]
    public void UpdateBeforeApproval_WhenInDraftStatus_ShouldUpdateDecision()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);

        // Act
        decision.UpdateBeforeApproval(DecisionType.Suspension, "Resumo Atualizado", "Motivo Atualizado");

        // Assert
        decision.DecisionType.Should().Be(DecisionType.Suspension);
        decision.Summary.Should().Be("Resumo Atualizado");
        decision.Reasoning.Should().Be("Motivo Atualizado");
    }

    [Fact]
    public void UpdateBeforeApproval_WhenPendingApproval_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();

        // Act
        Action act = () => decision.UpdateBeforeApproval(DecisionType.Suspension, "Resumo Alterado", "Motivo Alterado");

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A decisão só pode ser alterada no estado Rascunho.");
    }

    [Fact]
    public void Approve_WhenPendingApproval_ShouldTransitionToApproved()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();

        // Act
        decision.Approve(_approvedByUserId, _now.AddHours(1));

        // Assert
        decision.Status.Should().Be(DecisionStatus.Approved);
        decision.ApprovedByUserId.Should().Be(_approvedByUserId);
        decision.ApprovedAt.Should().Be(_now.AddHours(1));
    }

    [Fact]
    public void Approve_WithDefaultApprovedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();

        // Act
        Action act = () => decision.Approve(_approvedByUserId, default);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de aprovação é inválida.");
    }

    [Fact]
    public void Reject_WhenPendingApproval_ShouldSetStatusToRejected()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();

        // Act
        decision.Reject("Insuficiência de provas");

        // Assert
        decision.Status.Should().Be(DecisionStatus.Rejected);
    }

    [Fact]
    public void Invalidate_WhenApproved_ShouldSetStatusToInvalidated()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();
        decision.Approve(_approvedByUserId, _now.AddHours(1));

        // Act
        decision.Invalidate("Surgimento de fatos novos");

        // Assert
        decision.Status.Should().Be(DecisionStatus.Invalidated);
    }

    [Fact]
    public void SubmitForApproval_WhenRejected_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();
        decision.Reject("Recusado");

        // Act
        Action act = () => decision.SubmitForApproval();

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas decisões no estado Rascunho podem ser submetidas para aprovação.");
    }

    [Fact]
    public void Approve_WhenRejected_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var decision = DisciplinaryDecision.Create(_caseId, DecisionType.FormalWarning, "Resumo", "Motivo", _now, _decidedByUserId);
        decision.SubmitForApproval();
        decision.Reject("Recusado");

        // Act
        Action act = () => decision.Approve(_approvedByUserId, _now.AddHours(1));

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Apenas decisões pendentes de aprovação podem ser aprovadas.");
    }
}
