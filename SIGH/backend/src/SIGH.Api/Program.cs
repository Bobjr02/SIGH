using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using Serilog;
using SIGH.Api.Middlewares;
using SIGH.Application;
using SIGH.Infrastructure;
using SIGH.Infrastructure.Logging;
using SIGH.Persistence;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;

// Configuração do Bootstrap Logger com Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando a aplicação SIGH.Api...");

    var builder = WebApplication.CreateBuilder(args);

    // Configuração do Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<CorrelationIdEnricher>()
        .WriteTo.Console()
        .WriteTo.File("logs/sigh-api-.log", rollingInterval: RollingInterval.Day));

    // Injeção de Dependências por Camada
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddPersistence(builder.Configuration);

    // Limite de upload (Storage:MaxUploadSizeBytes, padrão 25 MB) aplicado também no Kestrel e no
    // FormOptions, com folga para o envelope multipart. O tamanho exato do arquivo é validado no caso de uso.
    var maxUploadBytes = builder.Configuration.GetSection(SIGH.Application.Options.StorageOptions.SectionName)
        .Get<SIGH.Application.Options.StorageOptions>()?.MaxUploadSizeBytes
        ?? SIGH.Application.Options.StorageOptions.DefaultMaxUploadSizeBytes;
    var maxRequestBodyBytes = maxUploadBytes + 1024 * 1024;
    builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = maxRequestBodyBytes);
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(form =>
    {
        form.MultipartBodyLengthLimit = maxRequestBodyBytes;
        form.ValueCountLimit = 64;
    });

    // Dados de demonstração: Seed:DemoData só em Development/Demo; em outro ambiente a API não sobe.
    builder.Services.AddOptions<SIGH.Api.Demo.DemoDataOptions>()
        .Bind(builder.Configuration.GetSection(SIGH.Api.Demo.DemoDataOptions.SectionName))
        .Validate(o => SIGH.Api.Demo.DemoDataGuard.IsAllowed(o, builder.Environment.EnvironmentName), SIGH.Api.Demo.DemoDataGuard.RefusedMessage)
        .Validate(o => !o.DemoData || !string.IsNullOrWhiteSpace(o.DemoPassword), "Seed:DemoData=true exige Seed:DemoPassword (senha dos usuários de demonstração).")
        .ValidateOnStart();
    builder.Services.AddScoped<SIGH.Api.Demo.DemoDataSeeder>();

    // Atrás de um proxy reverso (nginx do container web): IP real e esquema vêm dos cabeçalhos X-Forwarded-*.
    // ReverseProxy:KnownProxies (IPs separados por vírgula) restringe quem pode enviá-los; sem lista, confia
    // em qualquer origem — só use assim quando a API não for acessível diretamente (caso do compose.app.yml).
    var reverseProxyEnabled = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
    if (reverseProxyEnabled)
    {
        builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            var known = builder.Configuration["ReverseProxy:KnownProxies"];
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var ip in (known ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                options.KnownProxies.Add(System.Net.IPAddress.Parse(ip));
        });
    }

    // Suporte a ProblemDetails
    builder.Services.AddProblemDetails();

    // Rate Limiting (10 req/min por IP no login)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        // Login: RateLimits:Login:PermitLimit por minuto e por IP (padrão 10).
        var loginPermits = builder.Configuration.GetValue("RateLimits:Login:PermitLimit", 10);
        options.AddPolicy("LoginIpRateLimit", httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });

        // Recuperação de senha: limite próprio por IP (RateLimits:ForgotPassword / RateLimits:ResetPassword).
        // Dificulta usar o endpoint para disparar e-mails em massa ou adivinhar tokens.
        foreach (var (policy, section, defaultPermits) in new[]
                 {
                     ("ForgotPasswordRateLimit", "RateLimits:ForgotPassword", 5),
                     ("ResetPasswordRateLimit", "RateLimits:ResetPassword", 10)
                 })
        {
            var permits = builder.Configuration.GetValue($"{section}:PermitLimit", defaultPermits);
            var window = TimeSpan.FromMinutes(builder.Configuration.GetValue($"{section}:WindowMinutes", 15));
            options.AddPolicy(policy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));
        }
    });

    // Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });

    // Controllers e OpenAPI
    builder.Services.AddControllers();
    builder.Services.AddSingleton<SecurityStampCache>();
    builder.Services.AddEndpointsApiExplorer();

    // Health Checks (API + SQL Server / DbContext)
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SighDbContext>("Database");

    // CORS: origens vêm de Cors:AllowedOrigins. Em desenvolvimento o front usa o proxy do Vite
    // (mesma origem), então a lista pode ficar vazia; sem origens configuradas, nenhuma é liberada.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configuração do Swagger v1 com JWT e Tags
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SIGH API - Sistema Inteligente de Gestão de Histórico Disciplinar",
            Version = "v1",
            Description = "API oficial do sistema SIGH. Arquitetura padronizada v1 (Clean Architecture & DDD)."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o token JWT neste formato: Bearer {seu_token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Falha no startup se Storage:Local:RootPath estiver ausente ou dentro do diretório da aplicação.
    app.Services.GetRequiredService<SIGH.Application.Interfaces.Storage.IFileStorage>();

    // Fail-fast: valida JwtOptions e ConnectionStrings (ValidateOnStart) antes das
    // migrations, para que configuração ausente gere uma mensagem clara e não um erro de conexão.
    app.Services.GetService<Microsoft.Extensions.Options.IStartupValidator>()?.Validate();

    // Migrations + seed na inicialização (habilitado em Development).
    // Provedores não relacionais (ex.: InMemory nos testes) são ignorados.
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SighDbContext>();

        if (dbContext.Database.IsRelational())
        {
            Log.Information("Aplicando migrations do banco de dados...");
            await dbContext.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            Log.Information("Migrations e seed aplicados com sucesso.");

            if (scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SIGH.Api.Demo.DemoDataOptions>>().Value.DemoData)
                await scope.ServiceProvider.GetRequiredService<SIGH.Api.Demo.DemoDataSeeder>().SeedAsync();
        }
    }

    // Middlewares em Ordem de Execução
    if (reverseProxyEnabled)
        app.UseForwardedHeaders();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseRateLimiter();
    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIGH API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    // Perda de acesso imediata: stamp/status do usuário e vencimento de perfis conferidos a cada requisição.
    app.UseMiddleware<SecurityStampMiddleware>();
    // Troca de senha pendente: só change-password, logout e refresh ficam acessíveis.
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Health Checks Endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/api/v1/health-check");

    // Endpoint de Verificação da Fundação do Sistema (/api/v1)
    app.MapGet("/api/v1", () => Results.Ok(new
    {
        System = "SIGH - Sistema Inteligente de Gestão de Histórico Disciplinar",
        Version = "v1",
        Status = "Online",
        Environment = app.Environment.EnvironmentName,
        Documentation = "/swagger"
    }));

    Log.Information("Aplicação SIGH.Api v1 iniciada com sucesso.");
    app.Run();
}
catch (Exception ex) when (TryGetConfigurationFailures(ex, out var failures))
{
    Log.Fatal(
        "Configuração inválida: a aplicação SIGH.Api não pode iniciar.{NewLine}{Failures}",
        Environment.NewLine,
        string.Join(Environment.NewLine, failures.Select(f => " - " + f)));
    Environment.ExitCode = 1;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação SIGH.Api encerrou inesperadamente.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

// ValidateOnStart agrega as falhas de todas as opções num AggregateException.
static bool TryGetConfigurationFailures(Exception ex, out IReadOnlyList<string> failures)
{
    var validationErrors = ex is AggregateException aggregate
        ? aggregate.Flatten().InnerExceptions.ToList()
        : new List<Exception> { ex };

    if (validationErrors.Count > 0 && validationErrors.All(e => e is Microsoft.Extensions.Options.OptionsValidationException))
    {
        failures = validationErrors
            .Cast<Microsoft.Extensions.Options.OptionsValidationException>()
            .SelectMany(e => e.Failures)
            .ToList();
        return true;
    }

    failures = Array.Empty<string>();
    return false;
}

public partial class Program { }
