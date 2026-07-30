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

    // Suporte a ProblemDetails
    builder.Services.AddProblemDetails();

    // Rate Limiting (10 req/min por IP no login)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("LoginIpRateLimit", httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });
    });

    // Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });

    // Controllers e OpenAPI
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Health Checks (API + SQL Server / DbContext)
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SighDbContext>("Database");

    // Configuração do CORS para o Frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
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

    // Middlewares em Ordem de Execução
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
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação SIGH.Api encerrou inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
