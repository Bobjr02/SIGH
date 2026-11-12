namespace SIGH.Api.Middlewares;

/// <summary>
/// Libera o endpoint para usuários com troca de senha pendente
/// (claim <c>pwd_change_required</c>). Ver <see cref="PasswordChangeRequiredMiddleware"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class AllowWhilePasswordChangeRequiredAttribute : Attribute
{
}
