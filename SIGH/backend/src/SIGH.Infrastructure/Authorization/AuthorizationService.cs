using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Authorization;

public class AuthorizationService : ISighAuthorizationService
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
    {
        if (user == null || !user.Identity?.IsAuthenticated == true)
            return false;

        var result = await _authorizationService.AuthorizeAsync(user, null, new PermissionRequirement(permission));
        return result.Succeeded;
    }
}
