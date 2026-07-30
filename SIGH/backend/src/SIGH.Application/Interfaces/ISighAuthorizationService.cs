using System.Security.Claims;

namespace SIGH.Application.Interfaces;

public interface ISighAuthorizationService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
}
