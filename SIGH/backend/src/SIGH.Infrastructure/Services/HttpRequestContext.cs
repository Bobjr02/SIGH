using Microsoft.AspNetCore.Http;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Services;

public sealed class HttpRequestContext : IRequestContext
{
    /// <summary>Chave em HttpContext.Items onde o CorrelationIdMiddleware guarda o id.</summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Length > 500 ? value[..500] : value;
        }
    }

    public string? CorrelationId =>
        _accessor.HttpContext?.Items.TryGetValue(CorrelationIdItemKey, out var id) == true ? id?.ToString() : null;
}
