using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;

public record CreateInfractionTypeRequest(
    string Code,
    string Name,
    InfractionSeverity DefaultSeverity,
    string? Description = null,
    string? LegalReference = null);

public record CreateInfractionTypeResponse(
    Guid Id,
    string Code,
    string Name,
    InfractionSeverity DefaultSeverity,
    bool IsActive);
