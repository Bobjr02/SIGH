namespace SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;

public record ActivateInfractionTypeRequest(Guid Id);
public record ActivateInfractionTypeResponse(Guid Id, bool IsActive);
