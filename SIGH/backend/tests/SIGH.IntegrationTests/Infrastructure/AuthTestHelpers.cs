using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SIGH.Api.Contracts.Auth;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>Set-Cookie do refresh token, já decomposto em atributos.</summary>
public sealed record RefreshCookie(string Value, IReadOnlyDictionary<string, string?> Attributes, string Raw)
{
    public const string Name = "sigh_rt";

    public bool Has(string attribute) => Attributes.ContainsKey(attribute.ToLowerInvariant());

    public string? Get(string attribute) => Attributes.TryGetValue(attribute.ToLowerInvariant(), out var v) ? v : null;

    public bool IsDeletion =>
        string.IsNullOrEmpty(Value)
        || (DateTimeOffset.TryParse(Get("expires"), out var expires) && expires < DateTimeOffset.UtcNow);

    public static RefreshCookie? From(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers))
            return null;

        var raw = headers.LastOrDefault(h => h.StartsWith(Name + "=", StringComparison.Ordinal));
        if (raw is null)
            return null;

        var parts = raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var value = parts[0][(Name.Length + 1)..];
        var attributes = parts.Skip(1)
            .Select(p => p.Split('=', 2))
            .ToDictionary(kv => kv[0].ToLowerInvariant(), kv => kv.Length > 1 ? kv[1] : null);

        return new RefreshCookie(value, attributes, raw);
    }
}

public static class AuthTestHelpers
{
    public static async Task<(LoginHttpResponse Body, RefreshCookie Cookie)> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<Result<LoginHttpResponse>>())!.Data!;
        var cookie = RefreshCookie.From(response);
        cookie.Should().NotBeNull("o login deve gravar o refresh token no cookie");
        return (body, cookie!);
    }

    /// <summary>Cliente sem cookie jar: o cookie (se houver) é enviado manualmente.</summary>
    public static HttpClient CreateClientWithoutCookieJar(CustomWebApplicationFactory factory, string? refreshToken = null, string? accessToken = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });

        if (refreshToken is not null)
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie.Name}={refreshToken}");
        if (accessToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}
