using Serilog.Context;

namespace SIGH.Api.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}
