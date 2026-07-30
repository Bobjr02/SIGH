namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;

public record GetInfractionTypesQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10);
