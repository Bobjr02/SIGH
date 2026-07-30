using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryCaseTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _openedByUserId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldCreateCaseInDraftStatus()
    {
        // Act
        var disciplinaryCase = DisciplinaryCase.Create(
            caseNumber: "PROC-2026-001",
            companyId: _companyId,
            title: "Uso indevido de recursos",
            description: "Investigação sobre acesso não autorizado a sistemas.",
            openedAt: _now,
            openedByUserId: _openedByUserId
        );

        // Assert
        disciplinaryCase.Should().NotBeNull();
        disciplinaryCase.CaseNumber.Should().Be("PROC-2026-001");
        disciplinaryCase.CompanyId.Should().Be(_companyId);
        disciplinaryCase.Title.Should().Be("Uso indevido de recursos");
        disciplinaryCase.Description.Should().Be("Investigação sobre acesso não autorizado a sistemas.");
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Draft);
        disciplinaryCase.Priority.Should().Be(DisciplinaryCasePriority.Normal);
        disciplinaryCase.OpenedAt.Should().Be(_now);
        disciplinaryCase.OpenedByUserId.Should().Be(_openedByUserId);
        disciplinaryCase.Occurrences.Should().BeEmpty();
        disciplinaryCase.Employees.Should().BeEmpty();
        disciplinaryCase.Evidences.Should().BeEmpty();
        disciplinaryCase.Measures.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDefaultOpenedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryCase.Create(
            caseNumber: "PROC-001",
            companyId: _companyId,
            title: "Título Válido",
            description: "Descrição Válida",
            openedAt: default,
            openedByUserId: _openedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de abertura é inválida.");
    }

    [Theory]
    [InlineData(DisciplinaryCasePriority.Undefined)]
    [InlineData((DisciplinaryCasePriority)999)]
    public void Create_WithInvalidPriority_ShouldThrowBusinessRuleValidationException(DisciplinaryCasePriority invalidPriority)
    {
        // Act
        Action act = () => DisciplinaryCase.Create(
            caseNumber: "PROC-001",
            companyId: _companyId,
            title: "Título Válido",
            description: "Descrição Válida",
            openedAt: _now,
            openedByUserId: _openedByUserId,
            priority: invalidPriority
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A prioridade é inválida.");
    }

    [Theory]
    [InlineData(DisciplinaryCaseStatus.Undefined)]
    [InlineData((DisciplinaryCaseStatus)999)]
    public void Create_WithInvalidInitialStatus_ShouldThrowBusinessRuleValidationException(DisciplinaryCaseStatus invalidStatus)
    {
        // Act
        Action act = () => DisciplinaryCase.Create(
            caseNumber: "PROC-001",
            companyId: _companyId,
            title: "Título Válido",
            description: "Descrição Válida",
            openedAt: _now,
            openedByUserId: _openedByUserId,
            initialStatus: invalidStatus
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O status inicial do processo disciplinar é inválido.");
    }

    [Fact]
    public void Open_FromDraft_ShouldTransitionToOpenStatus()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();

        // Act
        disciplinaryCase.Open();

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Open);
    }

    [Fact]
    public void StartInvestigation_FromDraftDirectly_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            occurrenceDate: _now.AddHours(-2),
            reportedAt: _now,
            description: "Falta injustificada",
            reportedByUserId: _openedByUserId,
            infractionTypeId: Guid.NewGuid(),
            severity: InfractionSeverity.Moderate
        );
        disciplinaryCase.AddOccurrence(occurrence);

        // Act - Attempting Draft -> UnderInvestigation directly
        Action act = () => disciplinaryCase.StartInvestigation();

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A investigação só pode ser iniciada a partir de um processo Aberto.");
    }

    [Fact]
    public void StartInvestigation_FromOpenStatus_ShouldTransitionToUnderInvestigation()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        disciplinaryCase.Open(); // Draft -> Open

        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            occurrenceDate: _now.AddHours(-2),
            reportedAt: _now,
            description: "Falta injustificada",
            reportedByUserId: _openedByUserId,
            infractionTypeId: Guid.NewGuid(),
            severity: InfractionSeverity.Moderate
        );
        disciplinaryCase.AddOccurrence(occurrence);

        // Act - Open -> UnderInvestigation
        disciplinaryCase.StartInvestigation();

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.UnderInvestigation);
    }

    [Fact]
    public void AddEmployee_WithSameRoleTwice_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        var employeeId = Guid.NewGuid();
        var emp1 = DisciplinaryCaseEmployee.Create(disciplinaryCase.Id, employeeId, CaseEmployeeRole.Accused);
        var emp2 = DisciplinaryCaseEmployee.Create(disciplinaryCase.Id, employeeId, CaseEmployeeRole.Accused);

        disciplinaryCase.AddEmployee(emp1);

        // Act
        Action act = () => disciplinaryCase.AddEmployee(emp2);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O funcionário já está associado ao processo com este papel.");
    }

    [Fact]
    public void RegisterDecision_FromAnotherCase_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var otherCaseId = Guid.NewGuid();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: otherCaseId,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão de outro processo",
            reasoning: "Motivação",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );

        // Act
        Action act = () => disciplinaryCase.RegisterDecision(decision);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A decisão pertence a outro processo disciplinar.");
    }

    [Fact]
    public void RegisterDecision_WhenActiveDecisionAlreadyExists_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision1 = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Primeira decisão",
            reasoning: "Motivação 1",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision1);

        var decision2 = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.Suspension,
            summary: "Segunda decisão",
            reasoning: "Motivação 2",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );

        // Act
        Action act = () => disciplinaryCase.RegisterDecision(decision2);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Já existe uma decisão ativa vinculada a este processo disciplinar.");
    }

    [Fact]
    public void RegisterDecision_WithDraftStatus_ShouldRemainInAwaitingDecisionStatus()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão em Rascunho",
            reasoning: "Motivação em análise",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );

        // Act
        disciplinaryCase.RegisterDecision(decision);

        // Assert
        disciplinaryCase.Decision.Should().Be(decision);
        disciplinaryCase.DecisionId.Should().Be(decision.Id);
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.AwaitingDecision);
    }

    [Fact]
    public void RegisterDecision_AfterPreviousDecisionRejected_ShouldAllowNewDecisionAndPreserveHistory()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision1 = DisciplinaryDecision.Create(disciplinaryCase.Id, DecisionType.FormalWarning, "D1", "M1", _now, _openedByUserId, DecisionStatus.Draft);
        disciplinaryCase.RegisterDecision(decision1);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.RejectDecision("Rejeitada por falta de embasamento.");

        var decision2 = DisciplinaryDecision.Create(disciplinaryCase.Id, DecisionType.Suspension, "D2", "M2", _now, _openedByUserId, DecisionStatus.Draft);

        // Act
        disciplinaryCase.RegisterDecision(decision2);

        // Assert
        disciplinaryCase.Decisions.Should().HaveCount(2);
        disciplinaryCase.Decision.Should().Be(decision2);
        disciplinaryCase.DecisionId.Should().Be(decision2.Id);
    }

    [Fact]
    public void ApproveDecision_ShouldTransitionStatusToDecided()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão em Rascunho",
            reasoning: "Motivação",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();

        // Act
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Decided);
        disciplinaryCase.Decision!.Status.Should().Be(DecisionStatus.Approved);
        disciplinaryCase.Decision.ApprovedByUserId.Should().Be(_openedByUserId);
    }

    [Fact]
    public void InvalidateDecision_ShouldReturnProcessToAwaitingDecision()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão Aprovada",
            reasoning: "Motivação",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Decided);

        // Act
        disciplinaryCase.InvalidateDecision("Erro processual identificado.");

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.AwaitingDecision);
        disciplinaryCase.Decision.Should().BeNull();
        disciplinaryCase.Decisions.First().Status.Should().Be(DecisionStatus.Invalidated);
    }

    [Fact]
    public void Complete_WithApprovedDecision_ShouldTransitionToCompleted()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Advertência formal",
            reasoning: "Motivação clara",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));

        // Act
        disciplinaryCase.Complete("Processo concluído com aplicação de advertência.", _now.AddDays(1));

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Completed);
        disciplinaryCase.ClosedAt.Should().Be(_now.AddDays(1));
        disciplinaryCase.ConclusionSummary.Should().Be("Processo concluído com aplicação de advertência.");
    }

    [Fact]
    public void Complete_WithDefaultClosedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Advertência formal",
            reasoning: "Motivação clara",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));

        // Act
        Action act = () => disciplinaryCase.Complete("Conclusão com data default", default);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de encerramento é inválida.");
    }

    [Fact]
    public void Complete_WhenNotInDecidedStatus_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();

        // Act
        Action act = () => disciplinaryCase.Complete("Tentativa em AwaitingDecision", _now.AddDays(1));

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O processo deve estar Decidido para poder ser concluído.");
    }

    [Fact]
    public void Cancel_WhenInOpenStatus_ShouldTransitionToCancelled()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();

        // Act
        disciplinaryCase.Cancel("Solicitação da diretoria.", _now.AddDays(1));

        // Assert
        disciplinaryCase.Status.Should().Be(DisciplinaryCaseStatus.Cancelled);
        disciplinaryCase.CancellationReason.Should().Be("Solicitação da diretoria.");
        disciplinaryCase.CancelledAt.Should().Be(_now.AddDays(1));
    }

    [Fact]
    public void Cancel_WithDefaultCancelledAt_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();

        // Act
        Action act = () => disciplinaryCase.Cancel("Solicitação da diretoria.", default);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de cancelamento é inválida.");
    }

    [Fact]
    public void CancelledCase_IsTerminal_CannotBeModified()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        disciplinaryCase.Cancel("Cancelado", _now);

        // Act & Assert
        Action actUpdate = () => disciplinaryCase.UpdateDetails("Novo Título", "Nova Descrição");
        actUpdate.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O processo disciplinar cancelado não pode ser alterado.");
    }

    [Fact]
    public void CompletedCase_IsFrozen_AllChildOperationsThrowException()
    {
        // Arrange
        var disciplinaryCase = CreateCompletedCase();
        var occId = disciplinaryCase.Occurrences.First().Id;
        var empId = disciplinaryCase.Employees.First().Id;

        // Act & Assert
        Action actOcc = () => disciplinaryCase.UpdateOccurrence(occId, "Tentativa de alteração pós conclusão");
        actOcc.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O processo disciplinar concluído não pode ser alterado.");

        Action actEmp = () => disciplinaryCase.RegisterEmployeeStatement(empId, "Novo depoimento", _now);
        actEmp.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O processo disciplinar concluído não pode ser alterado.");
    }

    [Fact]
    public void AddOccurrence_FromAnotherCase_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        var otherCaseId = Guid.NewGuid();
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: otherCaseId,
            occurrenceDate: _now.AddHours(-2),
            reportedAt: _now,
            description: "Ocorrência de outro processo",
            reportedByUserId: _openedByUserId,
            infractionTypeId: Guid.NewGuid(),
            severity: InfractionSeverity.Moderate
        );

        // Act
        Action act = () => disciplinaryCase.AddOccurrence(occurrence);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A ocorrência pertence a outro processo disciplinar.");
    }

    [Fact]
    public void AddEvidence_WithOccurrenceFromAnotherCase_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();
        var occurrenceOfAnotherCaseId = Guid.NewGuid();

        var evidence = DisciplinaryEvidence.CreateTextEvidence(
            disciplinaryCaseId: disciplinaryCase.Id,
            title: "Evidência Teste",
            description: "Descrição da evidência",
            collectedAt: _now,
            collectedByUserId: _openedByUserId,
            disciplinaryOccurrenceId: occurrenceOfAnotherCaseId
        );

        // Act
        Action act = () => disciplinaryCase.AddEvidence(evidence);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A ocorrência vinculada à evidência não pertence a este processo disciplinar.");
    }

    [Fact]
    public void AddMeasure_WithDecisionOfAnotherProcess_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão Aprovada",
            reasoning: "Motivação",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));

        var accusedEmpId = disciplinaryCase.Employees.First(e => e.Role == CaseEmployeeRole.Accused).EmployeeId;
        var wrongDecisionId = Guid.NewGuid();

        var measure = DisciplinaryMeasure.Create(
            disciplinaryCaseId: disciplinaryCase.Id,
            disciplinaryDecisionId: wrongDecisionId,
            employeeId: accusedEmpId,
            measureType: DisciplinaryMeasureType.WrittenWarning,
            reason: "Justificativa da medida",
            effectiveFrom: _now
        );

        // Act
        Action act = () => disciplinaryCase.AddMeasure(measure);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A medida disciplinar deve estar vinculada à decisão ativa do processo.");
    }

    [Fact]
    public void Cancel_WhenInDecidedStatus_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var disciplinaryCase = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(disciplinaryCase.Id, DecisionType.FormalWarning, "D1", "M1", _now, _openedByUserId, DecisionStatus.Draft);
        disciplinaryCase.RegisterDecision(decision);
        disciplinaryCase.SubmitDecisionForApproval();
        disciplinaryCase.ApproveDecision(_openedByUserId, _now.AddHours(1));

        // Act
        Action act = () => disciplinaryCase.Cancel("Cancelamento em Decidido", _now.AddDays(1));

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Não é possível cancelar um processo no estado Decidido. É necessário invalidar a decisão primeiro.");
    }

    [Fact]
    public void Collections_ShouldBeReadOnly()
    {
        // Arrange
        var disciplinaryCase = CreateValidCase();

        // Assert
        disciplinaryCase.Occurrences.Should().BeAssignableTo<IReadOnlyCollection<DisciplinaryOccurrence>>();
        disciplinaryCase.Employees.Should().BeAssignableTo<IReadOnlyCollection<DisciplinaryCaseEmployee>>();
        disciplinaryCase.Evidences.Should().BeAssignableTo<IReadOnlyCollection<DisciplinaryEvidence>>();
        disciplinaryCase.Measures.Should().BeAssignableTo<IReadOnlyCollection<DisciplinaryMeasure>>();
        disciplinaryCase.Decisions.Should().BeAssignableTo<IReadOnlyCollection<DisciplinaryDecision>>();
    }

    private DisciplinaryCase CreateValidCase()
    {
        return DisciplinaryCase.Create(
            caseNumber: "PROC-2026-001",
            companyId: _companyId,
            title: "Processo Teste",
            description: "Descrição do processo de teste",
            openedAt: _now,
            openedByUserId: _openedByUserId
        );
    }

    private DisciplinaryCase CreateValidCaseWithOccurrence()
    {
        var caseItem = CreateValidCase();
        caseItem.Open(); // Draft -> Open
        var occurrence = DisciplinaryOccurrence.Create(
            disciplinaryCaseId: caseItem.Id,
            occurrenceDate: _now.AddHours(-1),
            reportedAt: _now,
            description: "Ocorrência de teste",
            reportedByUserId: _openedByUserId,
            infractionTypeId: Guid.NewGuid(),
            severity: InfractionSeverity.Low
        );
        caseItem.AddOccurrence(occurrence);
        return caseItem;
    }

    private DisciplinaryCase CreateValidCaseReadyForDecision()
    {
        var caseItem = CreateValidCaseWithOccurrence();
        caseItem.StartInvestigation();
        var accused = DisciplinaryCaseEmployee.Create(caseItem.Id, Guid.NewGuid(), CaseEmployeeRole.Accused);
        caseItem.AddEmployee(accused);
        caseItem.SubmitForDecision();
        return caseItem;
    }

    private DisciplinaryCase CreateCompletedCase()
    {
        var caseItem = CreateValidCaseReadyForDecision();
        var decision = DisciplinaryDecision.Create(
            disciplinaryCaseId: caseItem.Id,
            decisionType: DecisionType.FormalWarning,
            summary: "Decisão Aprovada",
            reasoning: "Motivação",
            decidedAt: _now,
            decidedByUserId: _openedByUserId,
            initialStatus: DecisionStatus.Draft
        );
        caseItem.RegisterDecision(decision);
        caseItem.SubmitDecisionForApproval();
        caseItem.ApproveDecision(_openedByUserId, _now.AddHours(1));
        caseItem.Complete("Processo concluído.", _now.AddDays(1));
        return caseItem;
    }
}
