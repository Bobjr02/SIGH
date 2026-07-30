using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public record AddEvidenceRequest(
    Guid DisciplinaryCaseId,
    EvidenceType Type,
    string Description,
    Guid CollectedByUserId,
    DateTimeOffset CollectedAt,
    Guid? OccurrenceId = null,
    string? ReferenceCode = null,
    string? Location = null);

public record AddEvidenceResponse(
    Guid Id,
    Guid DisciplinaryCaseId,
    EvidenceType Type,
    EvidenceStatus Status,
    DateTimeOffset CollectedAt);
