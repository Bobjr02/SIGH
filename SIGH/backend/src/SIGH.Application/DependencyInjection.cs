using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.ForgotPassword;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Authentication.Logout;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Authentication.ResetPassword;
using SIGH.Application.Options;
using SIGH.Application.Users.ChangeStatus;
using SIGH.Application.Users.CreateUser;
using SIGH.Application.Users.GetUserById;
using SIGH.Application.Users.GetUsers;
using SIGH.Application.Users.UnlockUser;

namespace SIGH.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Configuração de FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Bind Options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PasswordOptions>(configuration.GetSection(PasswordOptions.SectionName));
        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<NotificationReminderOptions>(configuration.GetSection(NotificationReminderOptions.SectionName));
        services.Configure<BackgroundWorkerOptions>(configuration.GetSection(BackgroundWorkerOptions.SectionName));
        services.Configure<SIGH.Application.Disciplinary.Reports.Options.DisciplinaryReportOptions>(configuration.GetSection(SIGH.Application.Disciplinary.Reports.Options.DisciplinaryReportOptions.SectionName));

        // Registro dos Serviços da Camada de Aplicação (Vertical Slice)
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ILogoutService, LogoutService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
        services.AddScoped<IResetPasswordService, ResetPasswordService>();
        services.AddScoped<IChangePasswordService, ChangePasswordService>();
        services.AddScoped<ICreateUserService, CreateUserService>();
        services.AddScoped<IUnlockUserService, UnlockUserService>();
        services.AddScoped<IChangeUserStatusService, ChangeUserStatusService>();
        services.AddScoped<IGetUsersService, GetUsersService>();
        services.AddScoped<IGetUserByIdService, GetUserByIdService>();

        // Registro de Casos de Uso do Módulo de Funcionários
        services.AddScoped<SIGH.Application.Employees.CreateEmployee.ICreateEmployeeUseCase, SIGH.Application.Employees.CreateEmployee.CreateEmployeeUseCase>();
        services.AddScoped<SIGH.Application.Employees.UpdateEmployee.IUpdateEmployeeUseCase, SIGH.Application.Employees.UpdateEmployee.UpdateEmployeeUseCase>();
        services.AddScoped<SIGH.Application.Employees.ChangeOrganizationalAssignment.IChangeEmployeeOrganizationalAssignmentUseCase, SIGH.Application.Employees.ChangeOrganizationalAssignment.ChangeEmployeeOrganizationalAssignmentUseCase>();
        services.AddScoped<SIGH.Application.Employees.ChangeSupervisor.IChangeEmployeeSupervisorUseCase, SIGH.Application.Employees.ChangeSupervisor.ChangeEmployeeSupervisorUseCase>();
        services.AddScoped<SIGH.Application.Employees.ChangeStatus.IChangeEmployeeStatusUseCase, SIGH.Application.Employees.ChangeStatus.ChangeEmployeeStatusUseCase>();
        services.AddScoped<SIGH.Application.Employees.TerminateEmployee.ITerminateEmployeeUseCase, SIGH.Application.Employees.TerminateEmployee.TerminateEmployeeUseCase>();
        services.AddScoped<SIGH.Application.Employees.ReactivateEmployee.IReactivateEmployeeUseCase, SIGH.Application.Employees.ReactivateEmployee.ReactivateEmployeeUseCase>();
        services.AddScoped<SIGH.Application.Employees.LinkUser.ILinkEmployeeUserUseCase, SIGH.Application.Employees.LinkUser.LinkEmployeeUserUseCase>();
        services.AddScoped<SIGH.Application.Employees.UnlinkUser.IUnlinkEmployeeUserUseCase, SIGH.Application.Employees.UnlinkUser.UnlinkEmployeeUserUseCase>();
        services.AddScoped<SIGH.Application.Employees.GetEmployeeById.IGetEmployeeByIdUseCase, SIGH.Application.Employees.GetEmployeeById.GetEmployeeByIdUseCase>();
        services.AddScoped<SIGH.Application.Employees.GetEmployees.IGetEmployeesUseCase, SIGH.Application.Employees.GetEmployees.GetEmployeesUseCase>();

        // Registro de Casos de Uso do Módulo Disciplinar - InfractionTypes
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType.ICreateInfractionTypeUseCase, SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType.CreateInfractionTypeUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById.IGetInfractionTypeByIdUseCase, SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById.GetInfractionTypeByIdUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes.IGetInfractionTypesUseCase, SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes.GetInfractionTypesUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType.IUpdateInfractionTypeUseCase, SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType.UpdateInfractionTypeUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType.IActivateInfractionTypeUseCase, SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType.ActivateInfractionTypeUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType.IDeactivateInfractionTypeUseCase, SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType.DeactivateInfractionTypeUseCase>();

        // Registro de Casos de Uso do Módulo Disciplinar - DisciplinaryCases
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase.ICreateDisciplinaryCaseUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase.CreateDisciplinaryCaseUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase.IOpenCaseUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase.OpenCaseUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation.IStartInvestigationUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation.StartInvestigationUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision.ISubmitCaseForDecisionUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision.SubmitCaseForDecisionUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision.IApproveDecisionUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision.ApproveDecisionUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision.IRejectDecisionUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision.RejectDecisionUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById.IGetDisciplinaryCaseByIdUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById.GetDisciplinaryCaseByIdUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases.IGetDisciplinaryCasesUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases.GetDisciplinaryCasesUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence.IAddOccurrenceUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence.AddOccurrenceUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee.IAddEmployeeToCaseUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee.AddEmployeeToCaseUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence.IAddEvidenceUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence.AddEvidenceUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision.IRecordDecisionUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision.RecordDecisionUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure.IApplyMeasureUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure.ApplyMeasureUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase.ICancelDisciplinaryCaseUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase.CancelDisciplinaryCaseUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase.IConcludeDisciplinaryCaseUseCase, SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase.ConcludeDisciplinaryCaseUseCase>();

        // Registro de Casos de Uso do Módulo Disciplinar - Reports & Analytics
        services.AddScoped<SIGH.Application.Disciplinary.Reports.UseCases.IGetEmployeeDisciplinaryHistoryUseCase, SIGH.Application.Disciplinary.Reports.UseCases.GetEmployeeDisciplinaryHistoryUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.Reports.UseCases.IGetDisciplinaryDashboardUseCase, SIGH.Application.Disciplinary.Reports.UseCases.GetDisciplinaryDashboardUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.Reports.UseCases.IGetDisciplinaryMeasuresUseCase, SIGH.Application.Disciplinary.Reports.UseCases.GetDisciplinaryMeasuresUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.Reports.UseCases.IGetDisciplinaryCaseReportUseCase, SIGH.Application.Disciplinary.Reports.UseCases.GetDisciplinaryCaseReportUseCase>();
        services.AddScoped<SIGH.Application.Disciplinary.Reports.UseCases.IExportDisciplinaryCaseReportCsvUseCase, SIGH.Application.Disciplinary.Reports.UseCases.ExportDisciplinaryCaseReportCsvUseCase>();

        // Registro de Casos de Uso do Módulo de Notificações
        services.AddScoped<SIGH.Application.Notifications.UseCases.IGetUserNotificationsUseCase, SIGH.Application.Notifications.UseCases.GetUserNotificationsUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IGetUnreadNotificationsUseCase, SIGH.Application.Notifications.UseCases.GetUnreadNotificationsUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IGetUnreadCountUseCase, SIGH.Application.Notifications.UseCases.GetUnreadCountUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IMarkNotificationAsReadUseCase, SIGH.Application.Notifications.UseCases.MarkNotificationAsReadUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IMarkAllNotificationsAsReadUseCase, SIGH.Application.Notifications.UseCases.MarkAllNotificationsAsReadUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IProcessRecurringRemindersUseCase, SIGH.Application.Notifications.UseCases.ProcessRecurringRemindersUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IProcessDeadlineEscalationsUseCase, SIGH.Application.Notifications.UseCases.ProcessDeadlineEscalationsUseCase>();
        services.AddScoped<SIGH.Application.Notifications.UseCases.IProcessDeadlineMonitoringUseCase, SIGH.Application.Notifications.UseCases.ProcessDeadlineMonitoringUseCase>();

        return services;
    }
}
