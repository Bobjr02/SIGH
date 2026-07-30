using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;
using SIGH.Persistence.Repositories;
using SIGH.Persistence.Repositories.Disciplinary;

namespace SIGH.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<SighDbContext>(options =>
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(SighDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<SighDbContext>());

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
