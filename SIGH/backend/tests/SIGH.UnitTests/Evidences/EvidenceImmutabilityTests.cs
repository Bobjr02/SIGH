using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Evidences;

/// <summary>Regras de imutabilidade das evidências, no domínio.</summary>
public class EvidenceImmutabilityTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static (DisciplinaryCase Case, DisciplinaryEvidence Evidence) CaseWithPendingEvidence()
    {
        var caseObj = DisciplinaryCase.Create($"IMU-{Guid.NewGuid():N}"[..12], Guid.NewGuid(), "Processo", "Descrição", Now, UserId);
        var evidence = DisciplinaryEvidence.CreatePendingFileEvidence(caseObj.Id, EvidenceType.Document, "Doc", Now, UserId);
        caseObj.AddEvidence(evidence);
        return (caseObj, evidence);
    }

    private static void Attach(DisciplinaryCase caseObj, Guid evidenceId) =>
        caseObj.AttachEvidenceFile(evidenceId, "0123456789abcdef0123456789abcdef.pdf", "doc.pdf", "application/pdf", 10, Hash, UserId, Now);

    [Fact]
    public void AttachingAFile_RecordsWhoAndWhen()
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();

        Attach(caseObj, evidence.Id);

        evidence.HasFile.Should().BeTrue();
        evidence.IntegrityHash.Should().Be(Hash);
        evidence.FileUploadedByUserId.Should().Be(UserId);
        evidence.FileUploadedAt.Should().Be(Now);
    }

    [Fact]
    public void AnUploadedFile_CannotBeReplaced()
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();
        Attach(caseObj, evidence.Id);

        FluentActions.Invoking(() => Attach(caseObj, evidence.Id))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*não pode ser substituído*");
        FluentActions.Invoking(() => caseObj.EnsureEvidenceAcceptsFile(evidence.Id))
            .Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void CancelledCase_AcceptsNoNewEvidenceNoFileAndNoRemoval()
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();
        caseObj.Cancel("Cancelado no teste", Now);

        FluentActions.Invoking(() => caseObj.AddEvidence(DisciplinaryEvidence.CreatePendingFileEvidence(caseObj.Id, EvidenceType.Image, "Nova", Now, UserId)))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*cancelado*");
        FluentActions.Invoking(() => Attach(caseObj, evidence.Id)).Should().Throw<BusinessRuleValidationException>();
        FluentActions.Invoking(() => caseObj.RemoveEvidence(evidence.Id, "motivo", UserId, Now)).Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Removal_RequiresAReason(string? reason)
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();

        FluentActions.Invoking(() => caseObj.RemoveEvidence(evidence.Id, reason!, UserId, Now))
            .Should().Throw<BusinessRuleValidationException>().WithMessage("*motivo*");
        evidence.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Removal_IsLogicalAndRecordsWhoRemoved()
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();
        Attach(caseObj, evidence.Id);

        caseObj.RemoveEvidence(evidence.Id, "  Anexada por engano  ", UserId, Now);

        caseObj.Evidences.Should().Contain(evidence, "nada é removido da coleção: a exclusão é lógica");
        evidence.IsDeleted.Should().BeTrue();
        evidence.RemovalReason.Should().Be("Anexada por engano");
        evidence.DeletedBy.Should().Be(UserId);
        evidence.DeletedAt.Should().Be(Now);
        evidence.StorageReference.Should().NotBeNull("o arquivo continua referenciado");
    }

    [Fact]
    public void RemovedEvidence_CannotReceiveAFileOrBeRemovedAgain()
    {
        var (caseObj, evidence) = CaseWithPendingEvidence();
        caseObj.RemoveEvidence(evidence.Id, "motivo", UserId, Now);

        FluentActions.Invoking(() => Attach(caseObj, evidence.Id)).Should().Throw<BusinessRuleValidationException>();
        FluentActions.Invoking(() => caseObj.RemoveEvidence(evidence.Id, "de novo", UserId, Now)).Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void TextEvidence_CannotReceiveAFile()
    {
        var caseObj = DisciplinaryCase.Create($"TXT-{Guid.NewGuid():N}"[..12], Guid.NewGuid(), "Processo", "Descrição", Now, UserId);
        var text = DisciplinaryEvidence.CreateTextEvidence(caseObj.Id, "Depoimento", "Relato", Now, UserId);
        caseObj.AddEvidence(text);

        FluentActions.Invoking(() => Attach(caseObj, text.Id)).Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void PendingFileEvidence_CannotBeText()
    {
        FluentActions.Invoking(() => DisciplinaryEvidence.CreatePendingFileEvidence(Guid.NewGuid(), EvidenceType.Text, "x", Now, UserId))
            .Should().Throw<BusinessRuleValidationException>();
    }
}
