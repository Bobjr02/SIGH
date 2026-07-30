using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;

public record OpenCaseRequest(Guid DisciplinaryCaseId, Guid OpenedByUserId);
public record OpenCaseResponse(Guid DisciplinaryCaseId, DisciplinaryCaseStatus Status, DateTimeOffset OpenedAt);
