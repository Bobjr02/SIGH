using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Entities;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

/// <summary>Ajustes mínimos do domínio para a fachada de registro e aprovação de medidas.</summary>
public class RecordDomainAdjustmentsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _registrar = Guid.NewGuid();
    private readonly Guid _approver = Guid.NewGuid();
    private readonly Guid _employee = Guid.NewGuid();

    private DisciplinaryCase NewRecordAwaitingApproval(DisciplinaryMeasureType type = DisciplinaryMeasureType.WrittenWarning)
    {
        var record = DisciplinaryCase.Create("MED-2026-0001", Guid.NewGuid(), "Medida", "Descrição", Now, _registrar, kind: DisciplinaryCaseKind.Record);
        record.Open();
        record.AddEmployee(DisciplinaryCaseEmployee.Create(record.Id, _employee, CaseEmployeeRole.Accused, true, managementUnitIdAtTime: Guid.NewGuid()));
        record.AddOccurrence(DisciplinaryOccurrence.Create(record.Id, Now.AddDays(-1), Now, "Falta", _registrar, Guid.NewGuid(), InfractionSeverity.Moderate));
        record.StartInvestigation();
        record.SubmitForDecision();
        var decision = DisciplinaryDecision.Create(record.Id, DisciplinaryDecision.DecisionTypeFor(type), "Resumo", "Fundamentação", Now, _registrar);
        decision.ProposeMeasure(type, new DateOnly(2026, 11, 13),
            type == DisciplinaryMeasureType.Suspension ? new DateOnly(2026, 11, 16) : null,
            type == DisciplinaryMeasureType.Suspension ? new DateOnly(2026, 11, 18) : null);
        record.RegisterDecision(decision);
        record.SubmitDecisionForApproval();
        return record;
    }

    [Fact]
    public void ProposedSuspension_CountsBothEnds()
    {
        var record = NewRecordAwaitingApproval(DisciplinaryMeasureType.Suspension);
        record.Decision!.SuspensionDays.Should().Be(3);
        record.Decision.DecisionType.Should().Be(DecisionType.Suspension);
    }

    [Theory]
    [InlineData(DisciplinaryMeasureType.VerbalWarning, DecisionType.FormalWarning)]
    [InlineData(DisciplinaryMeasureType.WrittenWarning, DecisionType.FormalWarning)]
    [InlineData(DisciplinaryMeasureType.Suspension, DecisionType.Suspension)]
    [InlineData(DisciplinaryMeasureType.JustCauseTermination, DecisionType.JustCauseTermination)]
    public void EveryRegistrableMeasure_MapsToADecisionType(DisciplinaryMeasureType measure, DecisionType decision) =>
        DisciplinaryDecision.DecisionTypeFor(measure).Should().Be(decision);

    [Fact]
    public void ProposeMeasure_RequiresTheSuspensionPeriod_AndOnlyForSuspensions()
    {
        var decision = DisciplinaryDecision.Create(Guid.NewGuid(), DecisionType.Suspension, "R", "F", Now, _registrar);
        var date = new DateOnly(2026, 11, 13);

        decision.Invoking(d => d.ProposeMeasure(DisciplinaryMeasureType.Suspension, date))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*início e de fim*");
        decision.Invoking(d => d.ProposeMeasure(DisciplinaryMeasureType.Suspension, date, date.AddDays(3), date.AddDays(1)))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*anterior ao início*");
        decision.Invoking(d => d.ProposeMeasure(DisciplinaryMeasureType.WrittenWarning, date, date, date))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*Somente a suspensão*");
    }

    [Fact]
    public void Rejection_KeepsWhoWhenAndWhy_AndTheRegistrantStillCannotReview()
    {
        var record = NewRecordAwaitingApproval();

        record.Invoking(r => r.RejectDecision(_registrar, "x", Now)).Should().Throw<BusinessRuleValidationException>()
            .WithMessage("*não pode rejeitar*");

        var decision = record.Decision!;
        record.RejectDecision(_approver, "  Data errada.  ", Now);

        decision.Status.Should().Be(DecisionStatus.Rejected);
        decision.RejectedByUserId.Should().Be(_approver);
        decision.RejectedAt.Should().Be(Now);
        decision.RejectionReason.Should().Be("Data errada.");
        record.Decision.Should().BeNull("a rejeitada sai da decisão ativa e uma corrigida pode ser registrada");
        record.Status.Should().Be(DisciplinaryCaseStatus.AwaitingDecision);
    }

    [Fact]
    public void ACompletedRecord_StillAcceptsEvidence_ButACompletedProcessOrACancelledRecordDoNot()
    {
        var record = NewRecordAwaitingApproval();
        record.ApproveDecision(_approver, Now);
        var measure = DisciplinaryMeasure.Create(record.Id, record.Decision!.Id, _employee, DisciplinaryMeasureType.WrittenWarning, "Motivo", Now);
        record.AddMeasure(measure);
        record.ApplyMeasure(measure.Id, Now, _approver);
        record.Complete("Aprovada.", Now);

        var letter = DisciplinaryEvidence.CreatePendingFileEvidence(record.Id, EvidenceType.Document, "Carta assinada", Now, _registrar);
        record.Invoking(r => r.AddEvidence(letter)).Should().NotThrow();

        var process = DisciplinaryCase.Create("PROC-1", Guid.NewGuid(), "Processo", "Descrição", Now, _registrar);
        process.Cancel("Aberto por engano.", Now);
        var late = DisciplinaryEvidence.CreatePendingFileEvidence(process.Id, EvidenceType.Document, "Tarde", Now, _registrar);
        process.Invoking(p => p.AddEvidence(late)).Should().Throw<BusinessRuleValidationException>().WithMessage("*cancelado*");
    }

    [Fact]
    public void Validity_IsInclusiveAtTheStartAndExclusiveAtTheEnd()
    {
        var link = new UserRole { ValidFrom = Now, ValidUntil = Now.AddDays(10) };
        link.IsValidAt(Now.AddTicks(-1)).Should().BeFalse();
        link.IsValidAt(Now).Should().BeTrue();
        link.IsValidAt(Now.AddDays(10).AddTicks(-1)).Should().BeTrue();
        link.IsValidAt(Now.AddDays(10)).Should().BeFalse();
        link.NextChangeAfter(Now.AddDays(-1)).Should().Be(Now);
        link.NextChangeAfter(Now).Should().Be(Now.AddDays(10));
        link.NextChangeAfter(Now.AddDays(11)).Should().BeNull();
        new UserRole().IsValidAt(Now).Should().BeTrue("sem validade, vale sempre");

        FluentActions.Invoking(() => ValidityExtensions.EnsureValidityRange(Now, Now)).Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void SecurityStamp_ChangesOnRotation()
    {
        var user = new User();
        var before = user.SecurityStamp;
        before.Should().NotBeNullOrEmpty();
        user.RotateSecurityStamp();
        user.SecurityStamp.Should().NotBe(before);
    }
}
