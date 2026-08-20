using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryEvidenceTests
{
    private readonly Guid _caseId = Guid.NewGuid();
    private readonly Guid _collectedByUserId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateFileEvidence_WithValidData_ShouldCreateFileEvidence()
    {
        // Act
        var evidence = DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Image,
            title: "Foto do Local",
            storageReference: "s3://bucket/evidences/photo01.jpg",
            originalFileName: "photo01.jpg",
            contentType: "image/jpeg",
            fileSize: 1024,
            collectedAt: _now,
            collectedByUserId: _collectedByUserId,
            integrityHash: "a1b2c3d4e5f6"
        );

        // Assert
        evidence.Should().NotBeNull();
        evidence.DisciplinaryCaseId.Should().Be(_caseId);
        evidence.EvidenceType.Should().Be(EvidenceType.Image);
        evidence.Title.Should().Be("Foto do Local");
        evidence.StorageReference.Should().Be("s3://bucket/evidences/photo01.jpg");
        evidence.OriginalFileName.Should().Be("photo01.jpg");
        evidence.ContentType.Should().Be("image/jpeg");
        evidence.FileSize.Should().Be(1024);
        evidence.CollectedAt.Should().Be(_now);
        evidence.CollectedByUserId.Should().Be(_collectedByUserId);
        evidence.IntegrityHash.Should().Be("a1b2c3d4e5f6");
        evidence.Status.Should().Be(EvidenceStatus.PendingVerification);
    }

    [Fact]
    public void CreateTextEvidence_WithValidData_ShouldCreateTextEvidence()
    {
        // Act
        var evidence = DisciplinaryEvidence.CreateTextEvidence(
            disciplinaryCaseId: _caseId,
            title: "Relato textual de testemunha",
            description: "A testemunha relatou que o horário anotado estava incorreto.",
            collectedAt: _now,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        evidence.Should().NotBeNull();
        evidence.EvidenceType.Should().Be(EvidenceType.Text);
        evidence.Title.Should().Be("Relato textual de testemunha");
        evidence.Description.Should().Be("A testemunha relatou que o horário anotado estava incorreto.");
        evidence.StorageReference.Should().BeNull();
        evidence.Status.Should().Be(EvidenceStatus.PendingVerification);
    }

    [Fact]
    public void CreateFileEvidence_WithDefaultCollectedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Document,
            title: "Relatório PDF",
            storageReference: "s3://ref",
            originalFileName: "doc.pdf",
            contentType: "application/pdf",
            fileSize: 500,
            collectedAt: default,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data de coleta da evidência é inválida.");
    }

    [Theory]
    [InlineData(EvidenceType.Undefined)]
    [InlineData((EvidenceType)999)]
    public void CreateFileEvidence_WithInvalidEvidenceType_ShouldThrowBusinessRuleValidationException(EvidenceType invalidType)
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: invalidType,
            title: "Relatório PDF",
            storageReference: "s3://ref",
            originalFileName: "doc.pdf",
            contentType: "application/pdf",
            fileSize: 500,
            collectedAt: _now,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O tipo de evidência é inválido.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFileEvidence_WithEmptyStorageReference_ShouldThrowBusinessRuleValidationException(string? invalidStorageRef)
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Document,
            title: "Relatório PDF",
            storageReference: invalidStorageRef!,
            originalFileName: "doc.pdf",
            contentType: "application/pdf",
            fileSize: 500,
            collectedAt: _now,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A referência de armazenamento é obrigatória para evidências de arquivo.");
    }

    [Fact]
    public void CreateFileEvidence_WithNegativeFileSize_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Document,
            title: "Relatório PDF",
            storageReference: "s3://ref",
            originalFileName: "doc.pdf",
            contentType: "application/pdf",
            fileSize: -1,
            collectedAt: _now,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O tamanho do arquivo não pode ser negativo.");
    }

    [Fact]
    public void CreateFileEvidence_WithEmptyCollectedByUserId_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Document,
            title: "Relatório PDF",
            storageReference: "s3://ref",
            originalFileName: "doc.pdf",
            contentType: "application/pdf",
            fileSize: 100,
            collectedAt: _now,
            collectedByUserId: Guid.Empty
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O usuário responsável pela coleta é obrigatório.");
    }

    [Theory]
    [InlineData("C:\\Users\\Admin\\Desktop\\file.png")]
    [InlineData("/var/log/app/file.png")]
    public void CreateFileEvidence_WithLocalPathInFileName_ShouldThrowBusinessRuleValidationException(string localPathFileName)
    {
        // Act
        Action act = () => DisciplinaryEvidence.CreateFileEvidence(
            disciplinaryCaseId: _caseId,
            evidenceType: EvidenceType.Image,
            title: "Foto",
            storageReference: "s3://ref",
            originalFileName: localPathFileName,
            contentType: "image/png",
            fileSize: 100,
            collectedAt: _now,
            collectedByUserId: _collectedByUserId
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O nome do arquivo não pode conter caminho local.");
    }

    [Fact]
    public void MarkAsVerified_ShouldSetStatusToVerified()
    {
        // Arrange
        var evidence = DisciplinaryEvidence.CreateTextEvidence(_caseId, "Título", "Descrição", _now, _collectedByUserId);

        // Act
        evidence.MarkAsVerified();

        // Assert
        evidence.Status.Should().Be(EvidenceStatus.Verified);
    }

    [Fact]
    public void MarkAsRejected_ShouldSetStatusToRejected()
    {
        // Arrange
        var evidence = DisciplinaryEvidence.CreateTextEvidence(_caseId, "Título", "Descrição", _now, _collectedByUserId);

        // Act
        evidence.MarkAsRejected();

        // Assert
        evidence.Status.Should().Be(EvidenceStatus.Rejected);
    }
}
