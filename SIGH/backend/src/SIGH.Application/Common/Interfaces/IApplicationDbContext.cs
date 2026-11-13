using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Notifications.Entities;

namespace SIGH.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<DisciplinaryCase> DisciplinaryCases { get; }
    DbSet<DisciplinaryOccurrence> DisciplinaryOccurrences { get; }
    DbSet<DisciplinaryCaseEmployee> DisciplinaryCaseEmployees { get; }
    DbSet<DisciplinaryEvidence> DisciplinaryEvidences { get; }
    DbSet<DisciplinaryDecision> DisciplinaryDecisions { get; }
    DbSet<DisciplinaryMeasure> DisciplinaryMeasures { get; }
    DbSet<InfractionType> InfractionTypes { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<SIGH.Domain.Entities.PasswordResetToken> PasswordResetTokens { get; }
    DbSet<SIGH.Domain.Entities.EmailOutboxMessage> EmailOutbox { get; }
    DbSet<SIGH.Domain.Entities.User> Users { get; }
    DbSet<SIGH.Domain.Entities.Role> Roles { get; }
    DbSet<SIGH.Domain.Entities.UserRole> UserRoles { get; }
    DbSet<SIGH.Domain.Entities.UserManagementUnit> UserManagementUnits { get; }
    DbSet<SIGH.Domain.Entities.RefreshToken> RefreshTokens { get; }
    DbSet<SIGH.Domain.Employees.Entities.ManagementUnit> ManagementUnits { get; }
    DbSet<SIGH.Domain.Employees.Entities.Employee> Employees { get; }
    DbSet<SIGH.Domain.Employees.Entities.Department> Departments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
