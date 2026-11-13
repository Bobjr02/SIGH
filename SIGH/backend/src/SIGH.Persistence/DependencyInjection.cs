using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;
using SIGH.Persistence.Options;
using SIGH.Persistence.Repositories;
using SIGH.Persistence.Repositories.Disciplinary;

namespace SIGH.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.DefaultConnection),
                "ConnectionStrings:DefaultConnection não configurada. " +
                "Em desenvolvimento use 'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<connection string>\"' no projeto SIGH.Api; " +
                "em CI/produção defina a variável de ambiente ConnectionStrings__DefaultConnection.")
            .ValidateOnStart();

        services.AddDbContext<SighDbContext>((serviceProvider, options) =>
            options.UseSqlServer(
                serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.DefaultConnection,
                b => b.MigrationsAssembly(typeof(SighDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<SighDbContext>());
        services.AddScoped<SIGH.Persistence.Seeds.DatabaseSeeder>();
        services.AddScoped<SIGH.Application.Security.ISecurityAuditLogger, SIGH.Persistence.Auditing.SecurityAuditLogger>();
        services.AddScoped<SIGH.Application.Security.IUserSecurityStateReader, UserSecurityStateReader>();
        services.AddScoped<SIGH.Application.Audit.IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<SIGH.Application.Email.IEmailOutbox, SIGH.Persistence.Email.EmailOutbox>();
        services.AddScoped<SIGH.Application.Employees.Import.IEmployeeImportStore, SIGH.Persistence.Repositories.Employees.EmployeeImportStore>();
        // Fontes do quadro de apoio à decisão (no sprint de histórico, entra a dos registros importados).
        services.AddScoped<SIGH.Application.Employees.DecisionSupport.IEmployeeMeasureSource, SIGH.Persistence.Repositories.Employees.SighMeasureSource>();
        services.AddScoped<SIGH.Application.Email.IEmailOutboxStore, SIGH.Persistence.Email.EmailOutboxStore>();

        // Registro de repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IManagementUnitRepository, ManagementUnitRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IJobTitleRepository, JobTitleRepository>();
        services.AddScoped<IDisciplinaryCaseRepository, DisciplinaryCaseRepository>();
        services.AddScoped<IInfractionTypeRepository, InfractionTypeRepository>();
        services.AddScoped<IEmployeeDisciplinaryHistoryQueryRepository, EmployeeDisciplinaryHistoryQueryRepository>();
        services.AddScoped<IDisciplinaryDashboardQueryRepository, DisciplinaryDashboardQueryRepository>();
        services.AddScoped<IDisciplinaryReportQueryRepository, DisciplinaryReportQueryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDeadlineMonitoringQueryRepository, DeadlineMonitoringQueryRepository>();
        services.AddScoped<SIGH.Application.Employees.Common.IEmployeeQueryService, SIGH.Persistence.Repositories.EmployeeQueryService>();
        services.AddScoped<SIGH.Application.Users.GetUsers.IUserQueryService, SIGH.Persistence.Repositories.UserQueryService>();

        return services;
    }
}
