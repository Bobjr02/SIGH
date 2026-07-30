using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Authentication;

public class CurrentUserService : ICurrentUserService
{
    // Placeholder para usuário autenticado (será preenchido na integração JWT/HttpContext)
    public Guid? UserId => null;
    public string? UserEmail => null;
    public bool IsAuthenticated => false;
}
