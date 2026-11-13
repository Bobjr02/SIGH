using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Authentication;

/// <summary>
/// Usuário autenticado da requisição atual, lido das claims do access token.
/// Fora de uma requisição HTTP (background workers, seed no startup) não há usuário: UserId é null.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User is not null;

    public bool HasPermission(string permission) =>
        User?.Claims.Any(c => (c.Type == "permission" || c.Type == "Permission")
                              && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase)) == true;
}
