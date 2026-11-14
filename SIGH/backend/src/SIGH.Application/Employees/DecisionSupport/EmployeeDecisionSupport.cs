using SIGH.Application.Interfaces;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.DecisionSupport;

/// <summary>Origem de uma medida no quadro de apoio à decisão.</summary>
public static class MeasureSources
{
    public const string Sigh = "SIGH";
    public const string Historical = "Histórico";
}

/// <summary>Uma medida aplicada a um funcionário, de qualquer origem (SIGH ou histórico importado).</summary>
/// <param name="SuspensionDays">Dias informados da suspensão; nulo = calcular pelo período (ou desconhecido, sem período).</param>
public sealed record MeasureFact(
    string Source,
    DisciplinaryMeasureType MeasureType,
    DateTimeOffset Date,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    IReadOnlyList<string> InfractionTypes,
    int? SuspensionDays = null)
{
    public bool IsHistorical => Source == MeasureSources.Historical;

    /// <summary>Dias da suspensão: os informados, senão o período (os dois extremos); nulo = desconhecido.</summary>
    public int? KnownSuspensionDays =>
        MeasureType != DisciplinaryMeasureType.Suspension ? null
        : SuspensionDays is { } days ? days
        : EffectiveFrom is { } from && EffectiveUntil is { } until && until >= from
            ? DateOnly.FromDateTime(until.Date).DayNumber - DateOnly.FromDateTime(from.Date).DayNumber + 1
            : null;
}

/// <summary>"2 advertências — 1 histórica, 1 no SIGH".</summary>
public sealed record OriginBreakdown(string Label, int Total, int Historical, int Sigh);

/// <summary>
/// Fonte de medidas aplicadas. Novas origens (ex.: registros históricos importados) entram registrando
/// outra implementação no DI; o cálculo soma todas as fontes.
/// </summary>
public interface IEmployeeMeasureSource
{
    Task<IReadOnlyList<MeasureFact>> GetAppliedMeasuresAsync(Guid employeeId, CancellationToken cancellationToken);
}

public sealed record CountByLabel(string Label, int Count);

public sealed record EmployeeDecisionSupportDto(
    Guid EmployeeId,
    int TotalMeasures,
    int Warnings,
    int VerbalWarnings,
    int WrittenWarnings,
    int Suspensions,
    int SuspensionDays,
    int SuspensionsWithoutPeriod,
    int OtherMeasures,
    IReadOnlyList<CountByLabel> ByType,
    IReadOnlyList<CountByLabel> ByInfractionType,
    DateTimeOffset? LastMeasureAt,
    int MeasuresLast6Months,
    int MeasuresLast12Months,
    IReadOnlyList<string> Sources,
    IReadOnlyList<OriginBreakdown> ByOrigin,
    int HistoricalMeasures,
    int SighMeasures);

/// <summary>
/// Quadro de apoio à decisão (só informativo). Regras:
/// - conta medidas aplicadas ou cumpridas (pendentes e canceladas ficam de fora; cada fonte já filtra);
/// - advertências = verbais + escritas;
/// - dias de suspensão = os dias informados da medida, senão os dias corridos de EffectiveFrom a EffectiveUntil
///   (incluindo os dois dias); suspensão sem dias nem período (dias desconhecidos) entra em
///   SuspensionsWithoutPeriod e não soma dias;
/// - origem separada: histórico importado × medidas aprovadas no SIGH (ByOrigin);
/// - por motivo: tipos de infração das ocorrências do processo da medida (a medida conta uma vez em
///   cada tipo distinto do processo; sem ocorrência, "Sem tipo de infração");
/// - janelas de 6 e 12 meses pela data da medida (aplicação), relativas a <c>now</c>.
/// </summary>
public static class EmployeeDecisionSupportCalculator
{
    public const string NoInfractionType = "Sem tipo de infração";

    public static EmployeeDecisionSupportDto Calculate(Guid employeeId, IReadOnlyList<MeasureFact> facts, DateTimeOffset now)
    {
        int Count(DisciplinaryMeasureType type) => facts.Count(f => f.MeasureType == type);

        var suspensions = facts.Where(f => f.MeasureType == DisciplinaryMeasureType.Suspension).ToList();
        // Dias desconhecidos (histórico sem dias nem período) não entram na soma: contam à parte.
        var suspensionDays = suspensions.Sum(f => f.KnownSuspensionDays ?? 0);

        var verbal = Count(DisciplinaryMeasureType.VerbalWarning);
        var written = Count(DisciplinaryMeasureType.WrittenWarning);

        var byType = facts.GroupBy(f => f.MeasureType)
            .Select(g => new CountByLabel(TypeLabel(g.Key), g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Label, StringComparer.Ordinal)
            .ToList();

        var byInfraction = facts
            .SelectMany(f => f.InfractionTypes.Count == 0 ? new[] { NoInfractionType } : f.InfractionTypes.Distinct().ToArray())
            .GroupBy(t => t)
            .Select(g => new CountByLabel(g.Key, g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Label, StringComparer.Ordinal)
            .ToList();

        return new EmployeeDecisionSupportDto(
            employeeId,
            facts.Count,
            verbal + written,
            verbal,
            written,
            suspensions.Count,
            suspensionDays,
            suspensions.Count(f => f.KnownSuspensionDays is null),
            facts.Count - verbal - written - suspensions.Count,
            byType,
            byInfraction,
            facts.Count == 0 ? null : facts.Max(f => f.Date),
            facts.Count(f => f.Date >= now.AddMonths(-6)),
            facts.Count(f => f.Date >= now.AddMonths(-12)),
            facts.Select(f => f.Source).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList(),
            ByOrigin(facts),
            facts.Count(f => f.IsHistorical),
            facts.Count(f => !f.IsHistorical));
    }

    private static IReadOnlyList<OriginBreakdown> ByOrigin(IReadOnlyList<MeasureFact> facts)
    {
        OriginBreakdown Of(string label, Func<MeasureFact, bool> match)
        {
            var group = facts.Where(match).ToList();
            return new OriginBreakdown(label, group.Count, group.Count(f => f.IsHistorical), group.Count(f => !f.IsHistorical));
        }

        var rows = new List<OriginBreakdown>
        {
            Of("Advertências", f => f.MeasureType is DisciplinaryMeasureType.VerbalWarning or DisciplinaryMeasureType.WrittenWarning),
            Of("Suspensões", f => f.MeasureType == DisciplinaryMeasureType.Suspension),
            Of("Demissões por justa causa", f => f.MeasureType == DisciplinaryMeasureType.JustCauseTermination),
            Of("Outras", f => f.MeasureType is not (DisciplinaryMeasureType.VerbalWarning or DisciplinaryMeasureType.WrittenWarning
                or DisciplinaryMeasureType.Suspension or DisciplinaryMeasureType.JustCauseTermination)),
        };
        return rows.Where(r => r.Total > 0).ToList();
    }

    public static string TypeLabel(DisciplinaryMeasureType type) => type switch
    {
        DisciplinaryMeasureType.Guidance => "Orientação",
        DisciplinaryMeasureType.VerbalWarning => "Advertência verbal",
        DisciplinaryMeasureType.WrittenWarning => "Advertência escrita",
        DisciplinaryMeasureType.Suspension => "Suspensão",
        DisciplinaryMeasureType.TerminationRecommendation => "Recomendação de desligamento",
        DisciplinaryMeasureType.JustCauseTermination => "Demissão por justa causa",
        _ => "Outra"
    };
}

public interface IEmployeeDecisionSupportService
{
    /// <summary>Null se o funcionário não existir.</summary>
    Task<EmployeeDecisionSupportDto?> GetAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public sealed class EmployeeDecisionSupportService : IEmployeeDecisionSupportService
{
    private readonly IEnumerable<IEmployeeMeasureSource> _sources;
    private readonly IDateTimeProvider _clock;
    private readonly IEmployeeRepository _employees;
    private readonly SIGH.Application.Common.Authorization.IAccessScope? _scope;

    public EmployeeDecisionSupportService(
        IEnumerable<IEmployeeMeasureSource> sources,
        IDateTimeProvider clock,
        IEmployeeRepository employees,
        SIGH.Application.Common.Authorization.IAccessScope? scope = null)
    {
        _sources = sources;
        _clock = clock;
        _employees = employees;
        _scope = scope;
    }

    public async Task<EmployeeDecisionSupportDto?> GetAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            return null;

        // Escopo por gerência: fora dele, o funcionário "não existe" (404).
        if (_scope is not null && !(await _scope.GetFilterAsync(cancellationToken)).Allows(employee.ManagementUnitId))
            return null;

        var facts = new List<MeasureFact>();
        foreach (var source in _sources)
            facts.AddRange(await source.GetAppliedMeasuresAsync(employeeId, cancellationToken));

        return EmployeeDecisionSupportCalculator.Calculate(employeeId, facts, _clock.UtcNow);
    }
}
