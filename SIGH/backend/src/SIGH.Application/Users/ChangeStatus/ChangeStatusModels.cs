using SIGH.Domain.Enums;

namespace SIGH.Application.Users.ChangeStatus;

public record ChangeUserStatusRequest(
    Guid UserId,
    UserStatus NewStatus,
    string Reason
);

public record ChangeUserStatusResponse(
    bool Success,
    string Message
);
