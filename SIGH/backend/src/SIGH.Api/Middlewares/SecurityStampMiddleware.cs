using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Application.Security;

namespace SIGH.Api.Middlewares;

/// <summary>
/// Cache em memória do estado de segurança dos usuários (stamp + status), com TTL de no máximo 30 s.
/// Usa o relógio da aplicação (IDateTimeProvider), então os testes controlam o TTL.
/// </summary>
public sealed class SecurityStampCache
{
    private readonly ConcurrentDictionary<Guid, (UserSecurityState? State, DateTimeOffset LoadedAt)> _entries = new();

    public bool TryGet(Guid userId, DateTimeOffset now, TimeSpan ttl, out UserSecurityState? state)
    {
        if (ttl > TimeSpan.Zero && _entries.TryGetValue(userId, out var entry) && now - entry.LoadedAt < ttl)
        {
            state = entry.State;
            return true;
        }

        state = null;
        return false;
    }

    public void Set(Guid userId, UserSecurityState? state, DateTimeOffset now) => _entries[userId] = (state, now);
}

/// <summary>
/// Perda de acesso imediata. Para todo token autenticado:
/// - o SecurityStamp do token tem de ser igual ao do banco (muda em desativação, bloqueio, senha,
///   perfis e gerências);
/// - o usuário não pode estar inativo, suspenso ou bloqueado;
/// - o instante <c>acc_chg</c> (próximo início/fim de vigência de perfil ou gerência) não pode ter passado.
/// Em qualquer desses casos a resposta é 401 e o front renova (o refresh recalcula as permissões ou é negado).
/// Deve rodar depois de UseAuthentication.
/// </summary>
public sealed class SecurityStampMiddleware
{
    public const string ErrorCode = "SESSION_REVOKED";
    public const string AccessChangedErrorCode = "ACCESS_CHANGED";

    private readonly RequestDelegate _next;

    public SecurityStampMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        SecurityStampCache cache,
        IUserSecurityStateReader reader,
        IDateTimeProvider clock,
        IOptions<AuthOptions> authOptions)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true || !RequiresCheck(context))
        {
            await _next(context);
            return;
        }

        var now = clock.UtcNow;

        if (long.TryParse(user.FindFirstValue(SighClaimTypes.AccessChangesAt), out var changesAtUnix)
            && now >= DateTimeOffset.FromUnixTimeSeconds(changesAtUnix))
        {
            await RejectAsync(context, AccessChangedErrorCode, "Os perfis ou gerências do usuário mudaram. Renove a sessão.");
            return;
        }

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await RejectAsync(context, ErrorCode, "Sessão inválida.");
            return;
        }

        var tokenStamp = user.FindFirstValue(SighClaimTypes.SecurityStamp);
        var ttl = TimeSpan.FromSeconds(Math.Clamp(authOptions.Value.SecurityStampCacheSeconds, 0, AuthOptions.MaxSecurityStampCacheSeconds));

        // O cache só confirma: se o token não bate com o valor em cache (por exemplo, um token novo
        // emitido logo depois de trocar a senha), o banco é relido antes de recusar.
        if (!cache.TryGet(userId, now, ttl, out var state) || !Accepts(state, tokenStamp))
        {
            state = await reader.GetAsync(userId, context.RequestAborted);
            cache.Set(userId, state, now);
        }

        if (!Accepts(state, tokenStamp))
        {
            await RejectAsync(context, ErrorCode, "Sessão revogada: o acesso do usuário mudou. Entre novamente.");
            return;
        }

        await _next(context);
    }

    private static bool Accepts(UserSecurityState? state, string? tokenStamp) =>
        state is { CanUseTheSystem: true }
        && !string.IsNullOrEmpty(tokenStamp)
        && string.Equals(tokenStamp, state.SecurityStamp, StringComparison.Ordinal);

    /// <summary>Só endpoints que exigem autenticação (login e refresh são anônimos), exceto o logout.</summary>
    private static bool RequiresCheck(HttpContext context)
    {
        var metadata = context.GetEndpoint()?.Metadata;
        if (metadata is null)
            return false;

        return metadata.GetMetadata<IAuthorizeData>() is not null
               && metadata.GetMetadata<IAllowAnonymous>() is null
               && metadata.GetMetadata<AllowRevokedSessionAttribute>() is null;
    }

    private static Task RejectAsync(HttpContext context, string code, string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Sessão inválida",
            detail: detail,
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["requestId"] = context.TraceIdentifier
            }).ExecuteAsync(context);
}
