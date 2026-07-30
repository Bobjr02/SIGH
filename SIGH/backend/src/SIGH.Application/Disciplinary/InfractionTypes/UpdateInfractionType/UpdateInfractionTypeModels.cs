using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;

public record UpdateInfractionTypeRequest(
    Guid Id,
    string Name,
    InfractionSeverity DefaultSeverity,
    bool RequiresFormalInvestigation,
    bool AllowsTerminationRecommendation,
    string? Description = null,
    string? LegalReference = null);

public record UpdateInfractionTypeResponse(
    Guid Id,
    string Code,
    string Name,
    InfractionSeverity DefaultSeverity,
    bool RequiresFormalInvestigation,
    bool AllowsTerminationRecommendation,
    string? Description,
    string? LegalReference,
    bool IsActive);
