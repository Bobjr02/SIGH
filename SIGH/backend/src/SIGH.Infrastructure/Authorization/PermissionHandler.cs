using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SIGH.Infrastructure.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User == null || !context.User.Identity?.IsAuthenticated == true)
        {
            return Task.CompletedTask;
        }

        var permissions = context.User.Claims
            .Where(c => c.Type == "permission" || c.Type == "Permission")
            .Select(c => c.Value);

        if (permissions.Any(p => string.Equals(p, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
