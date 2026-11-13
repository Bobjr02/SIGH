namespace SIGH.Api.Middlewares;

/// <summary>
/// Endpoint que aceita um access token de sessão revogada (stamp antigo). Só o logout: ele apenas
/// encerra a sessão e revoga o refresh token, então não há o que proteger.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowRevokedSessionAttribute : Attribute
{
}
