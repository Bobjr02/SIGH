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

        // Id externo é só para rastreio: limita o tamanho antes de reaproveitar em logs e auditoria.
        var id = correlationId.ToString();
        if (id.Length > 100) id = id[..100];

        context.Response.Headers[CorrelationIdHeaderName] = id;
        context.Items[SIGH.Infrastructure.Services.HttpRequestContext.CorrelationIdItemKey] = id;

        using (LogContext.PushProperty("CorrelationId", id))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}
