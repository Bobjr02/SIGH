using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.ForgotPassword;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.Logout;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Authentication.ResetPassword;
using SIGH.Application.Common.Models;

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

    public AuthController(
        ILoginService loginService,
        ILogoutService logoutService,
        IRefreshTokenService refreshTokenService,
        IForgotPasswordService forgotPasswordService,
        IResetPasswordService resetPasswordService,
        IChangePasswordService changePasswordService)
    {
        _loginService = loginService;
        _logoutService = logoutService;
        _refreshTokenService = refreshTokenService;
        _forgotPasswordService = forgotPasswordService;
        _resetPasswordService = resetPasswordService;
        _changePasswordService = changePasswordService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginIpRateLimit")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _loginService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return OkResult(result, "Autenticação realizada com sucesso.");
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _logoutService.LogoutAsync(userId, request, cancellationToken);
        return OkResult(result, "Logout realizado com sucesso.");
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Result<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<RefreshTokenResponse>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _refreshTokenService.RefreshTokenAsync(request, ipAddress, userAgent, cancellationToken);
        return OkResult(result, "Token renovado com sucesso.");
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<bool>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordService.ForgotPasswordAsync(request, cancellationToken);
        return OkResult(result, "Se o e-mail estiver cadastrado, as instruções de recuperação serão enviadas.");
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<bool>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _resetPasswordService.ResetPasswordAsync(request, cancellationToken);
        return OkResult(result, "Senha redefinida com sucesso.");
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<bool>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _changePasswordService.ChangePasswordAsync(userId, request, cancellationToken);
        return OkResult(result, "Senha alterada com sucesso.");
    }
}
