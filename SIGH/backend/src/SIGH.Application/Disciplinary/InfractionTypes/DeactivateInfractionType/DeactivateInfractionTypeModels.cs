namespace SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;

public record DeactivateInfractionTypeRequest(Guid Id);
public record DeactivateInfractionTypeResponse(Guid Id, bool IsActive);
