using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public record AddEvidenceRequest(
    Guid DisciplinaryCaseId,
    EvidenceType EvidenceType,
    string Title,
    Guid CollectedByUserId,
    DateTimeOffset CollectedAt,
    Guid? OccurrenceId = null,
    string? Description = null,
    string? StorageReference = null,
    string? OriginalFileName = null,
    string? ContentType = null,
    long? FileSize = null,
    string? IntegrityHash = null);

public record AddEvidenceResponse(
    Guid Id,
    Guid DisciplinaryCaseId,
    EvidenceType EvidenceType,
    EvidenceStatus Status,
    DateTimeOffset CollectedAt);
