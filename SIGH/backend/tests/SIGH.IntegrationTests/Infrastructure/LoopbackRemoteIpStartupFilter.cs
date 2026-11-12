using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>
/// O TestServer não preenche Connection.RemoteIpAddress (o Kestrel preenche). Define 127.0.0.1
/// para que IP, rate limit e auditoria se comportem como em produção.
/// </summary>
public sealed class LoopbackRemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
            await nextMiddleware();
        });
        next(app);
    };
}
