using Microsoft.AspNetCore.Authorization;
using SIGH.Application.Common.Authorization;

namespace SIGH.Api.Middlewares;

/// <summary>
/// Torna obrigatória, no servidor, a troca de senha do primeiro acesso: um token com a claim
/// <c>pwd_change_required</c> recebe 403 em qualquer endpoint que exija autenticação, exceto os
/// marcados com <see cref="AllowWhilePasswordChangeRequiredAttribute"/>
/// (change-password, logout e refresh).
/// Deve rodar depois de UseAuthentication e antes de UseAuthorization.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    public const string ErrorCode = "PASSWORD_CHANGE_REQUIRED";

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsBlocked(context))
        {
            var problem = Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Troca de senha obrigatória",
                detail: "É necessário trocar a senha antes de acessar este recurso. Use POST /api/v1/auth/change-password.",
                instance: context.Request.Path,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = ErrorCode,
                    ["requestId"] = context.TraceIdentifier
                });

            await problem.ExecuteAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsBlocked(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || !context.User.HasClaim(c => c.Type == SighClaimTypes.PasswordChangeRequired
                                           && string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var metadata = context.GetEndpoint()?.Metadata;
        if (metadata is null)
            return false;

        var requiresAuthorization = metadata.GetMetadata<IAuthorizeData>() is not null
                                    && metadata.GetMetadata<IAllowAnonymous>() is null;

        return requiresAuthorization
               && metadata.GetMetadata<AllowWhilePasswordChangeRequiredAttribute>() is null;
    }
}
