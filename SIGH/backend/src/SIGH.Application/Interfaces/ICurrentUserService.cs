namespace SIGH.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }

    /// <summary>A permissão está no access token da requisição atual.</summary>
    bool HasPermission(string permission) => false;
}
