using Microsoft.EntityFrameworkCore;
using SIGH.Application.Employees.DecisionSupport;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories.Employees;

/// <summary>Medidas aplicadas ou cumpridas registradas no próprio SIGH.</summary>
public sealed class SighMeasureSource : IEmployeeMeasureSource
{
    public const string SourceName = "SIGH";

    private readonly SighDbContext _context;

    public SighMeasureSource(SighDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MeasureFact>> GetAppliedMeasuresAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var measures = await _context.DisciplinaryMeasures.AsNoTracking()
            .Where(m => m.EmployeeId == employeeId
                        && (m.Status == DisciplinaryMeasureStatus.Applied || m.Status == DisciplinaryMeasureStatus.Completed))
            .Select(m => new { m.DisciplinaryCaseId, m.MeasureType, m.AppliedAt, m.EffectiveFrom, m.EffectiveUntil })
            .ToListAsync(cancellationToken);

        if (measures.Count == 0)
            return [];

        var caseIds = measures.Select(m => m.DisciplinaryCaseId).Distinct().ToList();
        var infractions = await (from o in _context.DisciplinaryOccurrences.AsNoTracking()
                                 join t in _context.InfractionTypes.AsNoTracking() on o.InfractionTypeId equals t.Id
                                 where caseIds.Contains(o.DisciplinaryCaseId)
                                 select new { o.DisciplinaryCaseId, t.Name })
            .ToListAsync(cancellationToken);
        var byCase = infractions.GroupBy(i => i.DisciplinaryCaseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(i => i.Name).Distinct().ToList());

        return measures.Select(m => new MeasureFact(
            SourceName,
            m.MeasureType,
            m.AppliedAt ?? m.EffectiveFrom,
            m.EffectiveFrom,
            m.EffectiveUntil,
            byCase.TryGetValue(m.DisciplinaryCaseId, out var names) ? names : [])).ToList();
    }
}
