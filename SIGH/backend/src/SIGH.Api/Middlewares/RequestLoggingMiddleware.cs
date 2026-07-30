using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace SIGH.Api.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var method = context.Request.Method;
        var path = context.Request.Path;

        await _next(context);

        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "Anonymous";
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("UserEmail", userEmail))
        using (LogContext.PushProperty("ClientIp", ipAddress))
        using (LogContext.PushProperty("ElapsedMilliseconds", stopwatch.ElapsedMilliseconds))
        {
            _logger.LogInformation(
                "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMs}ms - Usuário: {UserEmail} ({UserId}) - IP: {ClientIp} - CorrelationId: {CorrelationId}",
                method, path, statusCode, stopwatch.ElapsedMilliseconds, userEmail, userId, ipAddress, correlationId);
        }
    }
}
