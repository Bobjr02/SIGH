using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using SIGH.Api.Authentication;
using SIGH.Api.Contracts.Auth;
using SIGH.Api.Middlewares;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.CurrentUser;
using SIGH.Application.Authentication.ForgotPassword;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.Logout;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Authentication.ResetPassword;
using SIGH.Application.Common.Models;
using SIGH.Domain.Exceptions;

namespace SIGH.Api.Controllers;

[Route("api/v1/auth")]
[Tags("Authentication")]
public class AuthController : BaseController
{
    private readonly ILoginService _loginService;
    private readonly ILogoutService _logoutService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IForgotPasswordService _forgotPasswordService;
    private readonly IResetPasswordService _resetPasswordService;
    private readonly IChangePasswordService _changePasswordService;
    private readonly IGetCurrentUserService _getCurrentUserService;

    public AuthController(
        ILoginService loginService,
        ILogoutService logoutService,
        IRefreshTokenService refreshTokenService,
        IForgotPasswordService forgotPasswordService,
        IResetPasswordService resetPasswordService,
        IChangePasswordService changePasswordService,
        IGetCurrentUserService getCurrentUserService)
    {
        _loginService = loginService;
        _logoutService = logoutService;
        _refreshTokenService = refreshTokenService;
        _forgotPasswordService = forgotPasswordService;
        _resetPasswordService = resetPasswordService;
        _changePasswordService = changePasswordService;
        _getCurrentUserService = getCurrentUserService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginIpRateLimit")]
    [ProducesResponseType(typeof(Result<LoginHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<LoginHttpResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var updatedRequest = request with
        {
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _loginService.LoginAsync(updatedRequest, cancellationToken);
        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
        return OkResult(LoginHttpResponse.From(result), "Autenticação realizada com sucesso.");
    }

    /// <summary>
    /// Revoga o refresh token do cookie (e a sessão informada, se houver) e apaga o cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [AllowWhilePasswordChangeRequired]
    [ProducesResponseType(typeof(Result<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<LogoutResponse>>> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutHttpRequest? request,
        CancellationToken cancellationToken)
    {
        var updatedRequest = (request ?? new LogoutHttpRequest()).ToApplicationRequest(GetUserId(), RefreshTokenCookie.Read(Request));
        var result = await _logoutService.LogoutAsync(updatedRequest, cancellationToken);
        RefreshTokenCookie.Delete(Response);
        return OkResult(result, "Logout realizado com sucesso.");
    }

    /// <summary>
    /// Renova o access token usando o refresh token do cookie sigh_rt. O corpo da requisição é ignorado.
    /// </summary>
    [HttpPost("refresh")]
    [AllowWhilePasswordChangeRequired]
    [ProducesResponseType(typeof(Result<RefreshHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<RefreshHttpResponse>>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(Request);
        if (refreshToken is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Não Autorizado",
                detail: "Sessão ausente ou expirada. Faça login novamente.");
        }

        RefreshTokenResponse result;
        try
        {
            result = await _refreshTokenService.RefreshTokenAsync(
                new RefreshTokenRequest(refreshToken, GetClientIpAddress(), Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
        catch (BusinessRuleValidationException)
        {
            // Token inválido, expirado ou reutilizado: o cookie não serve mais.
            RefreshTokenCookie.Delete(Response);
            throw;
        }

        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
        return OkResult(RefreshHttpResponse.From(result), "Token renovado com sucesso.");
    }

    /// <summary>
    /// Dados do usuário autenticado. Perfis e permissões vêm do banco, não do token.
    /// Liberado durante a troca de senha obrigatória.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [AllowWhilePasswordChangeRequired]
    [ProducesResponseType(typeof(Result<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<CurrentUserResponse>>> Me(CancellationToken cancellationToken)
    {
        var result = await _getCurrentUserService.GetAsync(GetUserId(), cancellationToken);
        return OkResult(result);
    }

    /// <summary>
    /// Sempre a mesma resposta genérica, exista ou não o e-mail (sem enumeração). O link vai por e-mail
    /// (outbox), com token de uso único válido por Auth:PasswordResetTokenMinutes.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPasswordRateLimit")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Result<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<ForgotPasswordResponse>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordService.ForgotPasswordAsync(request, cancellationToken);
        return OkResult(result, "Se o e-mail estiver cadastrado, as instruções de recuperação serão enviadas.");
    }

    /// <summary>Redefine a senha com o token do link. Revoga todas as sessões e refresh tokens do usuário.</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("ResetPasswordRateLimit")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Result<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ResetPasswordResponse>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _resetPasswordService.ResetPasswordAsync(request, cancellationToken);
        return OkResult(result, "Senha redefinida com sucesso.");
    }

    [HttpPost("change-password")]
    [Authorize]
    [AllowWhilePasswordChangeRequired]
    [ProducesResponseType(typeof(Result<ChangePasswordHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<ChangePasswordHttpResponse>>> ChangePassword([FromBody] ChangePasswordHttpRequest request, CancellationToken cancellationToken)
    {
        // Devolve um novo access token sem a claim pwd_change_required e grava o novo refresh token
        // no cookie; os refresh tokens anteriores são revogados.
        var updatedRequest = request.ToApplicationRequest(GetUserId(), GetClientIpAddress(), Request.Headers.UserAgent.ToString());
        var result = await _changePasswordService.ChangePasswordAsync(updatedRequest, cancellationToken);
        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
        return OkResult(ChangePasswordHttpResponse.From(result), "Senha alterada com sucesso.");
    }

    private string GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
}
