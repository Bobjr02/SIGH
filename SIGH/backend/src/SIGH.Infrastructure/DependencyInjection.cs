using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtOptions.SecretKey) 
                        ? "TemporaryDefaultSecretKeyForInfrastructureBootstrap2026!" 
                        : jwtOptions.SecretKey))
            };
        });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddScoped<ISighAuthorizationService, AuthorizationService>();

        // Registro de serviços de infraestrutura
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthorizedCompanyProvider, AuthorizedCompanyProvider>();
        services.AddScoped<IEmailService, NullEmailService>();
        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDeadlineCalculator, DeadlineCalculator>();
        services.AddSingleton<IBackgroundJobScheduler, BackgroundJobScheduler>();

        // Background Workers
        services.AddHostedService<SIGH.Infrastructure.BackgroundWorkers.DisciplinaryDeadlineBackgroundWorker>();

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
