using SIGH.Application.Interfaces;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Employees.DecisionSupport;

/// <summary>Uma medida aplicada a um funcionário, de qualquer origem (SIGH hoje; histórico importado no próximo sprint).</summary>
public sealed record MeasureFact(
    string Source,
    DisciplinaryMeasureType MeasureType,
    DateTimeOffset Date,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    IReadOnlyList<string> InfractionTypes);

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
    IReadOnlyList<string> Sources);

/// <summary>
/// Quadro de apoio à decisão (só informativo). Regras:
/// - conta medidas aplicadas ou cumpridas (pendentes e canceladas ficam de fora; cada fonte já filtra);
/// - advertências = verbais + escritas;
/// - dias de suspensão = dias corridos de EffectiveFrom a EffectiveUntil, incluindo os dois dias;
///   suspensão sem data final entra em SuspensionsWithoutPeriod e não soma dias;
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
        var suspensionDays = suspensions
            .Where(f => f.EffectiveFrom.HasValue && f.EffectiveUntil.HasValue && f.EffectiveUntil >= f.EffectiveFrom)
            .Sum(f => DateOnly.FromDateTime(f.EffectiveUntil!.Value.Date).DayNumber - DateOnly.FromDateTime(f.EffectiveFrom!.Value.Date).DayNumber + 1);

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
            suspensions.Count(f => !f.EffectiveFrom.HasValue || !f.EffectiveUntil.HasValue),
            facts.Count - verbal - written - suspensions.Count,
            byType,
            byInfraction,
            facts.Count == 0 ? null : facts.Max(f => f.Date),
            facts.Count(f => f.Date >= now.AddMonths(-6)),
            facts.Count(f => f.Date >= now.AddMonths(-12)),
            facts.Select(f => f.Source).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList());
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
