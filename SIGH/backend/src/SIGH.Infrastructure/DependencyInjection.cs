using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Infrastructure.Authentication;
using SIGH.Infrastructure.Authorization;
using SIGH.Infrastructure.Email;
using SIGH.Infrastructure.Services;
using SIGH.Infrastructure.Storage;

namespace SIGH.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SecretKey) && Encoding.UTF8.GetByteCount(o.SecretKey) >= JwtOptions.MinimumSecretKeyBytes,
                $"JwtOptions:SecretKey ausente ou com menos de {JwtOptions.MinimumSecretKeyBytes} bytes (UTF-8). " +
                "Em desenvolvimento use 'dotnet user-secrets set \"JwtOptions:SecretKey\" \"<valor>\"' no projeto SIGH.Api; " +
                "em CI/produção defina a variável de ambiente JwtOptions__SecretKey.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Issuer),
                "JwtOptions:Issuer não configurado (variável de ambiente JwtOptions__Issuer).")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Audience),
                "JwtOptions:Audience não configurado (variável de ambiente JwtOptions__Audience).")
            .ValidateOnStart();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        // Os parâmetros de validação do token vêm das JwtOptions já validadas:
        // sem segredo de fallback, a API não sobe com configuração inválida.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddScoped<ISighAuthorizationService, AuthorizationService>();

        // Registro de serviços de infraestrutura
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddScoped<IAuthorizedCompanyProvider, AuthorizedCompanyProvider>();
        // E-mail: SMTP (MailKit) com Email:Enabled=true; senão NullEmailService (aviso no startup).
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(o => !o.Enabled || (!string.IsNullOrWhiteSpace(o.Smtp.Host) && !string.IsNullOrWhiteSpace(o.From)),
                "Email:Enabled=true exige Email:Smtp:Host e Email:From.")
            .ValidateOnStart();
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .Validate(o => string.IsNullOrWhiteSpace(o.PublicBaseUrl) || Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                "App:PublicBaseUrl deve ser uma URL absoluta (ex.: https://sigh.empresa.com.br).")
            .ValidateOnStart();
        if (configuration.GetValue<bool>($"{EmailOptions.SectionName}:Enabled"))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, NullEmailService>();
        services.AddScoped<SIGH.Application.Email.EmailOutboxProcessor>();
        services.AddHostedService<EmailOutboxDispatcher>();
        services.AddSingleton<SIGH.Application.Interfaces.Storage.IFileStorage, LocalFileStorage>();
        services.AddSingleton<SIGH.Application.Interfaces.Storage.IMalwareScanner, NoOpMalwareScanner>();
        services.AddSingleton<SIGH.Infrastructure.Import.ExcelHrSpreadsheetReader>();
        services.AddSingleton<SIGH.Application.Employees.Import.IHrSpreadsheetReader>(sp => sp.GetRequiredService<SIGH.Infrastructure.Import.ExcelHrSpreadsheetReader>());
        services.AddSingleton<SIGH.Application.Employees.Import.ISpreadsheetTableReader>(sp => sp.GetRequiredService<SIGH.Infrastructure.Import.ExcelHrSpreadsheetReader>());
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDeadlineCalculator, DeadlineCalculator>();
        services.AddSingleton<IBackgroundJobScheduler, BackgroundJobScheduler>();

        // Background Workers
        services.AddHostedService<SIGH.Infrastructure.BackgroundWorkers.DisciplinaryDeadlineBackgroundWorker>();
        services.AddHostedService<SIGH.Infrastructure.BackgroundWorkers.PasswordResetRequestWorker>();
        services.AddHostedService<SIGH.Infrastructure.BackgroundWorkers.RecordPendingAlertWorker>();

        // Registros de Segurança e Criptografia
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IRandomStringGenerator, RandomStringGenerator>();

        return services;
    }
}
