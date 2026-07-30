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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
