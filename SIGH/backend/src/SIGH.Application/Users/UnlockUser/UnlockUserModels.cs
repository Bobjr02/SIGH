namespace SIGH.Application.Users.UnlockUser;

public record UnlockUserRequest(
    Guid UserId,
    Guid UnlockedByUserId
);

public record UnlockUserResponse(
    bool Success,
    string Message
);
