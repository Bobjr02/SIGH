using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Notifications.Entities;

namespace SIGH.Persistence.Context;

public class SighDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Módulo de Funcionários
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ManagementUnit> ManagementUnits => Set<ManagementUnit>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Módulo Disciplinar
    public DbSet<DisciplinaryCase> DisciplinaryCases => Set<DisciplinaryCase>();
    public DbSet<DisciplinaryOccurrence> DisciplinaryOccurrences => Set<DisciplinaryOccurrence>();
    public DbSet<DisciplinaryCaseEmployee> DisciplinaryCaseEmployees => Set<DisciplinaryCaseEmployee>();
    public DbSet<DisciplinaryEvidence> DisciplinaryEvidences => Set<DisciplinaryEvidence>();
    public DbSet<DisciplinaryDecision> DisciplinaryDecisions => Set<DisciplinaryDecision>();
    public DbSet<DisciplinaryMeasure> DisciplinaryMeasures => Set<DisciplinaryMeasure>();
    public DbSet<InfractionType> InfractionTypes => Set<InfractionType>();

    // Módulo de Notificações
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    public SighDbContext(DbContextOptions<SighDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SighDbContext).Assembly);

        // Aplica o Global Query Filter de Soft Delete em todas as entidades auditáveis
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IAuditableEntity.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var expression = Expression.Lambda(Expression.Equal(property, falseConstant), parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(expression);
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;

                case EntityState.Deleted:
                    // Soft Delete Automático: nunca excluir fisicamente
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = currentUserId;
                    break;
            }
        }
    }
}
