using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryEvidence : AuditableEntity
{
    public Guid DisciplinaryCaseId { get; private set; }
    public Guid? DisciplinaryOccurrenceId { get; private set; }
    public EvidenceType EvidenceType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? StorageReference { get; private set; }
    public string? OriginalFileName { get; private set; }
    public string? ContentType { get; private set; }
    public long? FileSize { get; private set; }
    public DateTimeOffset CollectedAt { get; private set; }
    public Guid CollectedByUserId { get; private set; }
    public string? IntegrityHash { get; private set; }
    public EvidenceStatus Status { get; private set; } = EvidenceStatus.PendingVerification;

    /// <summary>Quando e por quem o arquivo foi enviado (o usuário vem do token).</summary>
    public DateTimeOffset? FileUploadedAt { get; private set; }
    public Guid? FileUploadedByUserId { get; private set; }

    /// <summary>Motivo da remoção lógica. A evidência nunca é excluída fisicamente.</summary>
    public string? RemovalReason { get; private set; }

    public bool HasFile => !string.IsNullOrEmpty(StorageReference);

    // EF Core
    protected DisciplinaryEvidence() : base() { }

    protected DisciplinaryEvidence(Guid id) : base(id) { }

    public static DisciplinaryEvidence CreateFileEvidence(
        Guid disciplinaryCaseId,
        EvidenceType evidenceType,
        string title,
        string storageReference,
        string originalFileName,
        string contentType,
        long fileSize,
        DateTimeOffset collectedAt,
        Guid collectedByUserId,
        string? integrityHash = null,
        Guid? disciplinaryOccurrenceId = null,
        string? description = null)
    {
        ValidateCommon(disciplinaryCaseId, title, collectedByUserId, collectedAt);

        if (evidenceType == EvidenceType.Undefined || !Enum.IsDefined(evidenceType))
            throw new BusinessRuleValidationException("O tipo de evidência é inválido.");

        if (string.IsNullOrWhiteSpace(storageReference))
            throw new BusinessRuleValidationException("A referência de armazenamento é obrigatória para evidências de arquivo.");

        if (fileSize < 0)
            throw new BusinessRuleValidationException("O tamanho do arquivo não pode ser negativo.");

        if (originalFileName.Contains('/') || originalFileName.Contains('\\'))
            throw new BusinessRuleValidationException("O nome do arquivo não pode conter caminho local.");

        var trimmedFileName = originalFileName.Trim();
        if (trimmedFileName.Length > DisciplinaryDomainConstants.OriginalFileNameMaxLength)
            throw new BusinessRuleValidationException($"O nome original do arquivo não pode exceder {DisciplinaryDomainConstants.OriginalFileNameMaxLength} caracteres.");

        var trimmedStorageRef = storageReference.Trim();
        if (trimmedStorageRef.Length > DisciplinaryDomainConstants.StorageReferenceMaxLength)
            throw new BusinessRuleValidationException($"A referência de armazenamento não pode exceder {DisciplinaryDomainConstants.StorageReferenceMaxLength} caracteres.");

        var trimmedContentType = contentType.Trim();
        if (trimmedContentType.Length > DisciplinaryDomainConstants.ContentTypeMaxLength)
            throw new BusinessRuleValidationException($"O tipo de conteúdo não pode exceder {DisciplinaryDomainConstants.ContentTypeMaxLength} caracteres.");

        if (integrityHash?.Length > DisciplinaryDomainConstants.IntegrityHashMaxLength)
            throw new BusinessRuleValidationException($"O hash de integridade não pode exceder {DisciplinaryDomainConstants.IntegrityHashMaxLength} caracteres.");

        return new DisciplinaryEvidence
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            DisciplinaryOccurrenceId = disciplinaryOccurrenceId,
            EvidenceType = evidenceType,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            StorageReference = trimmedStorageRef,
            OriginalFileName = trimmedFileName,
            ContentType = trimmedContentType,
            FileSize = fileSize,
            CollectedAt = collectedAt,
            CollectedByUserId = collectedByUserId,
            IntegrityHash = string.IsNullOrWhiteSpace(integrityHash) ? null : integrityHash.Trim(),
            Status = EvidenceStatus.PendingVerification
        };
    }

    /// <summary>
    /// Evidência de arquivo cujo conteúdo será anexado depois (upload multipart).
    /// Os metadados do arquivo (nome, tipo, tamanho, hash) só são definidos pelo servidor em <see cref="AttachFile"/>.
    /// </summary>
    public static DisciplinaryEvidence CreatePendingFileEvidence(
        Guid disciplinaryCaseId,
        EvidenceType evidenceType,
        string title,
        DateTimeOffset collectedAt,
        Guid collectedByUserId,
        Guid? disciplinaryOccurrenceId = null,
        string? description = null)
    {
        ValidateCommon(disciplinaryCaseId, title, collectedByUserId, collectedAt);

        if (evidenceType == EvidenceType.Undefined || evidenceType == EvidenceType.Text || !Enum.IsDefined(evidenceType))
            throw new BusinessRuleValidationException("O tipo de evidência é inválido para uma evidência de arquivo.");

        if (description?.Trim().Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        return new DisciplinaryEvidence
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            DisciplinaryOccurrenceId = disciplinaryOccurrenceId,
            EvidenceType = evidenceType,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CollectedAt = collectedAt,
            CollectedByUserId = collectedByUserId,
            Status = EvidenceStatus.PendingVerification
        };
    }

    public static DisciplinaryEvidence CreateTextEvidence(
        Guid disciplinaryCaseId,
        string title,
        string description,
        DateTimeOffset collectedAt,
        Guid collectedByUserId,
        Guid? disciplinaryOccurrenceId = null)
    {
        ValidateCommon(disciplinaryCaseId, title, collectedByUserId, collectedAt);

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("A descrição da evidência textual é obrigatória.");

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        return new DisciplinaryEvidence
        {
            DisciplinaryCaseId = disciplinaryCaseId,
            DisciplinaryOccurrenceId = disciplinaryOccurrenceId,
            EvidenceType = EvidenceType.Text,
            Title = title.Trim(),
            Description = trimmedDescription,
            CollectedAt = collectedAt,
            CollectedByUserId = collectedByUserId,
            Status = EvidenceStatus.PendingVerification
        };
    }

    private static void ValidateCommon(Guid disciplinaryCaseId, string title, Guid collectedByUserId, DateTimeOffset collectedAt)
    {
        if (disciplinaryCaseId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID do processo disciplinar é obrigatório.");

        if (collectedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário responsável pela coleta é obrigatório.");

        if (collectedAt == default)
            throw new BusinessRuleValidationException("A data de coleta da evidência é inválida.");

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("O título da evidência é obrigatório.");

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > DisciplinaryDomainConstants.EvidenceTitleMaxLength)
            throw new BusinessRuleValidationException($"O título da evidência não pode exceder {DisciplinaryDomainConstants.EvidenceTitleMaxLength} caracteres.");
    }

    /// <summary>
    /// Anexa o arquivo. Um arquivo enviado nunca é substituído: só é possível anexar uma vez.
    /// </summary>
    internal void AttachFile(
        string storageReference,
        string originalFileName,
        string contentType,
        long fileSize,
        string sha256Hash,
        Guid uploadedByUserId,
        DateTimeOffset uploadedAt)
    {
        if (IsDeleted)
            throw new BusinessRuleValidationException("Não é possível anexar arquivo a uma evidência removida.");

        if (EvidenceType == EvidenceType.Text)
            throw new BusinessRuleValidationException("Evidências do tipo Texto não recebem arquivo: o conteúdo delas é o próprio texto registrado (por exemplo, um depoimento digitado).");

        if (HasFile)
            throw new BusinessRuleValidationException("Esta evidência já possui arquivo. Um arquivo enviado não pode ser substituído.");

        if (string.IsNullOrWhiteSpace(storageReference) || storageReference.Length > DisciplinaryDomainConstants.StorageReferenceMaxLength)
            throw new BusinessRuleValidationException("A referência de armazenamento é inválida.");

        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Contains('/') || originalFileName.Contains('\\')
            || originalFileName.Length > DisciplinaryDomainConstants.OriginalFileNameMaxLength)
            throw new BusinessRuleValidationException("O nome original do arquivo é inválido.");

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > DisciplinaryDomainConstants.ContentTypeMaxLength)
            throw new BusinessRuleValidationException("O tipo de conteúdo do arquivo é inválido.");

        if (fileSize <= 0)
            throw new BusinessRuleValidationException("O arquivo está vazio.");

        if (string.IsNullOrWhiteSpace(sha256Hash) || sha256Hash.Length != 64)
            throw new BusinessRuleValidationException("O hash SHA-256 do arquivo é inválido.");

        if (uploadedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário que enviou o arquivo é obrigatório.");

        StorageReference = storageReference;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSize = fileSize;
        IntegrityHash = sha256Hash.ToLowerInvariant();
        FileUploadedByUserId = uploadedByUserId;
        FileUploadedAt = uploadedAt;
    }

    /// <summary>Remoção lógica: motivo obrigatório e registro de quem removeu.</summary>
    internal void Remove(string reason, Guid removedByUserId, DateTimeOffset removedAt)
    {
        if (IsDeleted)
            throw new BusinessRuleValidationException("A evidência já foi removida.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleValidationException("O motivo da remoção da evidência é obrigatório.");

        var trimmed = reason.Trim();
        if (trimmed.Length > DisciplinaryDomainConstants.EvidenceRemovalReasonMaxLength)
            throw new BusinessRuleValidationException($"O motivo da remoção não pode exceder {DisciplinaryDomainConstants.EvidenceRemovalReasonMaxLength} caracteres.");

        if (removedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário que removeu a evidência é obrigatório.");

        RemovalReason = trimmed;
        IsDeleted = true;
        DeletedAt = removedAt;
        DeletedBy = removedByUserId;
    }

    internal void UpdateDescription(string? description)
    {
        if (description?.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    internal void MarkAsVerified()
    {
        Status = EvidenceStatus.Verified;
    }

    internal void MarkAsRejected()
    {
        Status = EvidenceStatus.Rejected;
    }
}
