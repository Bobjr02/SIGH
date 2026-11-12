using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public record AddEvidenceRequest(
    Guid DisciplinaryCaseId,
    EvidenceType EvidenceType,
    string Title,
    Guid CollectedByUserId,
    DateTimeOffset CollectedAt,
    Guid? OccurrenceId = null,
    string? Description = null);

// Evidências de arquivo nascem sem arquivo: o conteúdo é enviado depois em
// POST /api/v1/disciplinary-cases/{id}/evidences/{evidenceId}/file (multipart), e só o servidor
// define nome, tipo, tamanho e hash.

public record AddEvidenceResponse(
    Guid Id,
    Guid DisciplinaryCaseId,
    EvidenceType EvidenceType,
    EvidenceStatus Status,
    DateTimeOffset CollectedAt);
