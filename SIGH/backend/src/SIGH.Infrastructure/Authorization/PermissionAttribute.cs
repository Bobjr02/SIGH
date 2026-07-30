using Microsoft.AspNetCore.Authorization;

namespace SIGH.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public PermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"{PolicyPrefix}{permission}";
    }

    public string Permission { get; }
}
