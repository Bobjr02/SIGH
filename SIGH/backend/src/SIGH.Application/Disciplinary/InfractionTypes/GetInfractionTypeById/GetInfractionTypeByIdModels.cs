using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;

public record GetInfractionTypeByIdResponse(
    Guid Id,
    string Code,
    string Name,
    InfractionSeverity DefaultSeverity,
    string? Description,
    string? LegalReference,
    bool IsActive);
