using System.Security;
using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.Records;

public interface IRecordService
{
    Task<RecordSaveResult> RegisterAsync(Guid registrantUserId, RecordInput input, CancellationToken cancellationToken = default);
    Task<RecordSaveResult> CorrectAsync(Guid userId, Guid recordId, RecordInput input, CancellationToken cancellationToken = default);
    Task<RecordDetailDto> ApproveAsync(Guid approverUserId, Guid recordId, CancellationToken cancellationToken = default);
    Task<RecordDetailDto> RejectAsync(Guid approverUserId, Guid recordId, ReasonInput input, CancellationToken cancellationToken = default);
    Task<RecordDetailDto> CancelAsync(Guid userId, Guid recordId, ReasonInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fachada do fluxo real do RH (registrar a medida já decidida → aprovar ou rejeitar). NÃO é um domínio
/// paralelo: cada operação executa, numa única transação (um SaveChanges), as etapas reais do
/// DisciplinaryCase — criar, abrir, vincular o acusado, registrar as ocorrências, iniciar a investigação,
/// encaminhar para decisão, registrar e submeter a decisão; e, na aprovação, aprovar a decisão, criar e
/// aplicar a medida e concluir o processo.
///
/// Justa causa NÃO altera o status do funcionário: o SAP é a fonte da verdade e a próxima importação
/// marca o desligamento.
/// </summary>
public sealed class RecordService : IRecordService
{
    public const int DescriptionMaxLength = 4000;

    private readonly IApplicationDbContext _db;
    private readonly IDisciplinaryCaseRepository _cases;
    private readonly IAuthorizedCompanyProvider _company;
    private readonly IDateTimeProvider _clock;
    private readonly IRecordNotifier _notifier;
    private readonly IRecordQueries _queries;

    public RecordService(
        IApplicationDbContext db,
        IDisciplinaryCaseRepository cases,
        IAuthorizedCompanyProvider company,
        IDateTimeProvider clock,
        IRecordNotifier notifier,
        IRecordQueries queries)
    {
        _db = db;
        _cases = cases;
        _company = company;
        _clock = clock;
        _notifier = notifier;
        _queries = queries;
    }

    public async Task<RecordSaveResult> RegisterAsync(Guid registrantUserId, RecordInput input, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var (employee, infraction, dates) = await ValidateAsync(input, now, currentRecordId: null, cancellationToken);

        var duplicates = await FindPossibleDuplicatesAsync(employee.Id, input.MeasureType, dates, currentRecordId: null, cancellationToken);
        if (duplicates.Count > 0 && !input.ConfirmPossibleDuplicate)
            return new RecordSaveResult(null, duplicates);

        var companyId = await _company.GetAuthorizedCompanyIdAsync(null, cancellationToken);
        var record = DisciplinaryCase.Create(
            caseNumber: NewCaseNumber(now),
            companyId: companyId,
            title: TitleOf(input.MeasureType, employee),
            description: input.Description,
            openedAt: now,
            openedByUserId: registrantUserId,
            initialStatus: DisciplinaryCaseStatus.Draft,
            kind: DisciplinaryCaseKind.Record);

        // Etapas reais do domínio, na ordem do processo.
        record.Open();
        record.AddEmployee(DisciplinaryCaseEmployee.Create(record.Id, employee.Id, CaseEmployeeRole.Accused, isPrimarySubject: true, managementUnitIdAtTime: employee.ManagementUnitId));
        foreach (var date in dates)
            record.AddOccurrence(NewOccurrence(record.Id, date, now, input.Description, registrantUserId, infraction));
        record.StartInvestigation();
        record.SubmitForDecision();
        RegisterDecision(record, input, registrantUserId, now);

        await _cases.AddAsync(record, cancellationToken);
        await _notifier.RecordRegisteredAsync(companyId, record.Id, record.CaseNumber, RecordLabels.Of(input.MeasureType), registrantUserId, resubmitted: false, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new RecordSaveResult(await DetailAsync(record.Id, cancellationToken), duplicates);
    }

    public async Task<RecordSaveResult> CorrectAsync(Guid userId, Guid recordId, RecordInput input, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var record = await LoadAsync(recordId, cancellationToken);
        EnsureRejected(record, "corrigida");

        var (employee, infraction, dates) = await ValidateAsync(input, now, recordId, cancellationToken);
        var duplicates = await FindPossibleDuplicatesAsync(employee.Id, input.MeasureType, dates, recordId, cancellationToken);
        if (duplicates.Count > 0 && !input.ConfirmPossibleDuplicate)
            return new RecordSaveResult(null, duplicates);

        record.UpdateDetails(TitleOf(input.MeasureType, employee), input.Description);

        // Funcionário trocado: o vínculo de acusado é substituído (com a gerência atual do novo).
        var accused = record.Employees.Single(e => e.Role == CaseEmployeeRole.Accused);
        if (accused.EmployeeId != employee.Id)
        {
            record.RemoveEmployee(accused.Id);
            record.AddEmployee(DisciplinaryCaseEmployee.Create(record.Id, employee.Id, CaseEmployeeRole.Accused, isPrimarySubject: true, managementUnitIdAtTime: employee.ManagementUnitId));
        }

        // Ocorrências: mantém as que continuam (mesma data e motivo), rejeita as que saíram e cria as novas.
        // Rejeitar (em vez de apagar) preserva a história; o domínio não remove ocorrências depois da investigação.
        var active = record.Occurrences.Where(o => o.Status != OccurrenceStatus.Rejected).ToList();
        var wanted = dates.Select(BusinessCalendar.StartOf).ToHashSet();
        foreach (var occurrence in active)
        {
            if (occurrence.InfractionTypeId == infraction.Id && wanted.Contains(occurrence.OccurrenceDate))
                record.UpdateOccurrence(occurrence.Id, input.Description);
            else
                record.RejectOccurrence(occurrence.Id);
        }
        var kept = active.Where(o => o.InfractionTypeId == infraction.Id && wanted.Contains(o.OccurrenceDate)).Select(o => o.OccurrenceDate).ToHashSet();
        foreach (var date in dates.Where(d => !kept.Contains(BusinessCalendar.StartOf(d))))
            record.AddOccurrence(NewOccurrence(record.Id, date, now, input.Description, userId, infraction));

        RegisterDecision(record, input, userId, now);

        await _notifier.RecordRegisteredAsync(record.CompanyId, record.Id, record.CaseNumber, RecordLabels.Of(input.MeasureType), userId, resubmitted: true, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new RecordSaveResult(await DetailAsync(record.Id, cancellationToken), duplicates);
    }

    public async Task<RecordDetailDto> ApproveAsync(Guid approverUserId, Guid recordId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var record = await LoadAsync(recordId, cancellationToken);
        var decision = EnsurePendingAndReviewable(record, approverUserId);

        // Aprovar a decisão (o domínio barra quem a registrou), criar e aplicar a medida e concluir.
        record.ApproveDecision(approverUserId, now);

        var accused = record.Employees.Single(e => e.Role == CaseEmployeeRole.Accused);
        var type = decision.ProposedMeasureType!.Value;
        var effectiveFrom = BusinessCalendar.StartOf(decision.SuspensionStart ?? decision.MeasureDate!.Value);
        DateTimeOffset? effectiveUntil = decision.SuspensionEnd is { } end ? BusinessCalendar.StartOf(end).AddDays(1).AddTicks(-1) : null;

        var measure = DisciplinaryMeasure.Create(record.Id, decision.Id, accused.EmployeeId, type, record.Description, effectiveFrom, effectiveUntil);
        record.AddMeasure(measure);
        record.ApplyMeasure(measure.Id, now, approverUserId);
        record.Complete($"{RecordLabels.Of(type)} aprovada.", now);

        await _notifier.RecordReviewedAsync(record.CompanyId, record.Id, record.CaseNumber, RecordLabels.Of(type), record.OpenedByUserId, approved: true, reason: null, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await DetailAsync(record.Id, cancellationToken);
    }

    public async Task<RecordDetailDto> RejectAsync(Guid approverUserId, Guid recordId, ReasonInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input?.Reason))
            throw new BusinessRuleValidationException("O motivo da rejeição é obrigatório.");

        var now = _clock.UtcNow;
        var record = await LoadAsync(recordId, cancellationToken);
        var decision = EnsurePendingAndReviewable(record, approverUserId);

        record.RejectDecision(approverUserId, input.Reason, now);

        await _notifier.RecordReviewedAsync(record.CompanyId, record.Id, record.CaseNumber, RecordLabels.Of(decision.ProposedMeasureType!.Value),
            record.OpenedByUserId, approved: false, reason: input.Reason.Trim(), cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await DetailAsync(record.Id, cancellationToken);
    }

    public async Task<RecordDetailDto> CancelAsync(Guid userId, Guid recordId, ReasonInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input?.Reason))
            throw new BusinessRuleValidationException("O motivo do cancelamento é obrigatório.");

        var record = await LoadAsync(recordId, cancellationToken);
        EnsureRejected(record, "cancelada");

        record.Cancel(input.Reason, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await DetailAsync(record.Id, cancellationToken);
    }

    // ---- regras ----

    private async Task<(Employee Employee, InfractionType Infraction, IReadOnlyList<DateOnly> Dates)> ValidateAsync(
        RecordInput input, DateTimeOffset now, Guid? currentRecordId, CancellationToken cancellationToken)
    {
        if (input is null)
            throw new BusinessRuleValidationException("Os dados da medida são obrigatórios.");

        if (!RecordLabels.RegistrableTypes.Contains(input.MeasureType))
            throw new BusinessRuleValidationException("Tipo de medida não aceito no registro. Use advertência verbal, advertência escrita, suspensão ou demissão por justa causa.");

        if (string.IsNullOrWhiteSpace(input.Description))
            throw new BusinessRuleValidationException("A descrição/observação é obrigatória.");
        if (input.Description.Trim().Length > DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição não pode exceder {DescriptionMaxLength} caracteres.");

        var dates = (input.OccurrenceDates ?? []).Distinct().Order().ToList();
        if (dates.Count == 0)
            throw new BusinessRuleValidationException("Informe pelo menos uma data de ocorrência.");

        var today = BusinessCalendar.Today(now);
        if (dates.Any(d => d > today))
            throw new BusinessRuleValidationException("Nenhuma data de ocorrência pode estar no futuro.");

        if (input.MeasureDate == default)
            throw new BusinessRuleValidationException("A data da medida é obrigatória.");
        if (input.MeasureDate < dates[^1])
            throw new BusinessRuleValidationException("A data da medida não pode ser anterior à última ocorrência.");

        if (input.MeasureType == DisciplinaryMeasureType.Suspension)
        {
            if (input.SuspensionStart is not { } start || input.SuspensionEnd is not { } end)
                throw new BusinessRuleValidationException("A suspensão exige data de início e de fim.");
            if (end < start)
                throw new BusinessRuleValidationException("O fim da suspensão não pode ser anterior ao início.");
            if (start < input.MeasureDate)
                throw new BusinessRuleValidationException("A suspensão não pode começar antes da data da medida.");
        }
        else if (input.SuspensionStart is not null || input.SuspensionEnd is not null)
        {
            throw new BusinessRuleValidationException("Somente a suspensão tem período de início e fim.");
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == input.EmployeeId, cancellationToken)
                       ?? throw new KeyNotFoundException("Funcionário não encontrado.");
        if (employee.Status == EmployeeStatus.Terminated)
            throw new BusinessRuleValidationException("Funcionário desligado não recebe medida.");

        var infraction = await _db.InfractionTypes.FirstOrDefaultAsync(i => i.Id == input.InfractionTypeId, cancellationToken)
                         ?? throw new BusinessRuleValidationException("Motivo (tipo de infração) não encontrado.");
        if (!infraction.IsActive)
            throw new BusinessRuleValidationException("O motivo informado está inativo.");

        if (input.MeasureType == DisciplinaryMeasureType.Suspension)
            await EnsureNoOverlappingSuspensionAsync(employee.Id, input.SuspensionStart!.Value, input.SuspensionEnd!.Value, currentRecordId, cancellationToken);

        return (employee, infraction, dates);
    }

    /// <summary>Medidas do funcionário pendentes ou aprovadas (as rejeitadas e canceladas não contam).</summary>
    private IQueryable<DisciplinaryCase> LiveRecordsOf(Guid employeeId, Guid? exceptRecordId) =>
        _db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.Kind == DisciplinaryCaseKind.Record && c.Id != exceptRecordId
                        && c.Employees.Any(e => e.Role == CaseEmployeeRole.Accused && e.EmployeeId == employeeId)
                        && (c.Status == DisciplinaryCaseStatus.Completed
                            || (c.Status == DisciplinaryCaseStatus.AwaitingDecision && c.Decisions.Any(d => d.Status == DecisionStatus.PendingApproval))));

    private async Task EnsureNoOverlappingSuspensionAsync(Guid employeeId, DateOnly start, DateOnly end, Guid? currentRecordId, CancellationToken cancellationToken)
    {
        var overlapping = await LiveRecordsOf(employeeId, currentRecordId)
            .SelectMany(c => c.Decisions.Where(d => (d.Status == DecisionStatus.PendingApproval || d.Status == DecisionStatus.Approved)
                                                    && d.ProposedMeasureType == DisciplinaryMeasureType.Suspension
                                                    && d.SuspensionStart <= end && d.SuspensionEnd >= start)
                .Select(d => c.CaseNumber))
            .FirstOrDefaultAsync(cancellationToken);

        if (overlapping is not null)
            throw new BusinessRuleValidationException($"A suspensão se sobrepõe a outra suspensão pendente ou aprovada deste funcionário ({overlapping}).");
    }

    private async Task<IReadOnlyList<PossibleDuplicateDto>> FindPossibleDuplicatesAsync(
        Guid employeeId, DisciplinaryMeasureType type, IReadOnlyList<DateOnly> dates, Guid? currentRecordId, CancellationToken cancellationToken)
    {
        var instants = dates.Select(BusinessCalendar.StartOf).ToList();
        var rows = await LiveRecordsOf(employeeId, currentRecordId)
            .Where(c => c.Decisions.Any(d => (d.Status == DecisionStatus.PendingApproval || d.Status == DecisionStatus.Approved) && d.ProposedMeasureType == type))
            .SelectMany(c => c.Occurrences
                .Where(o => o.Status != OccurrenceStatus.Rejected && instants.Contains(o.OccurrenceDate))
                .Select(o => new { c.Id, c.CaseNumber, c.Status, o.OccurrenceDate }))
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PossibleDuplicateDto(r.Id, r.CaseNumber,
                r.Status == DisciplinaryCaseStatus.Completed ? RecordStatus.Approved : RecordStatus.PendingApproval,
                BusinessCalendar.DateOf(r.OccurrenceDate)))
            .DistinctBy(d => (d.Id, d.OccurrenceDate))
            .ToList();
    }

    private static DisciplinaryDecision EnsurePendingAndReviewable(DisciplinaryCase record, Guid reviewerUserId)
    {
        var decision = record.Decision;
        if (record.Status != DisciplinaryCaseStatus.AwaitingDecision || decision?.Status != DecisionStatus.PendingApproval)
            throw new BusinessRuleValidationException("Só medidas aguardando aprovação podem ser aprovadas ou rejeitadas.");

        // Quem registra nunca aprova a mesma medida: vale para o registrante original e para quem corrigiu.
        // (O domínio já barra o autor da decisão; aqui também o autor do registro.)
        if (reviewerUserId == record.OpenedByUserId || reviewerUserId == decision.DecidedByUserId)
            throw new SecurityException("Quem registrou a medida não pode aprová-la nem rejeitá-la.");

        return decision;
    }

    private static void EnsureRejected(DisciplinaryCase record, string action)
    {
        var rejected = record.Status == DisciplinaryCaseStatus.AwaitingDecision && record.Decision is null
                       && record.Decisions.Any(d => d.Status == DecisionStatus.Rejected);
        if (!rejected)
            throw new BusinessRuleValidationException($"Só uma medida rejeitada pode ser {action}.");
    }

    private static void RegisterDecision(DisciplinaryCase record, RecordInput input, Guid decidedBy, DateTimeOffset now)
    {
        var label = RecordLabels.Of(input.MeasureType);
        var decision = DisciplinaryDecision.Create(record.Id, DisciplinaryDecision.DecisionTypeFor(input.MeasureType),
            summary: $"{label} registrada (decisão já tomada).", reasoning: input.Description, decidedAt: now, decidedByUserId: decidedBy);
        decision.ProposeMeasure(input.MeasureType, input.MeasureDate, input.SuspensionStart, input.SuspensionEnd);
        record.RegisterDecision(decision);
        record.SubmitDecisionForApproval();
    }

    private static DisciplinaryOccurrence NewOccurrence(Guid recordId, DateOnly date, DateTimeOffset now, string description, Guid reportedBy, InfractionType infraction) =>
        DisciplinaryOccurrence.Create(recordId, BusinessCalendar.StartOf(date), now, description, reportedBy, infraction.Id, infraction.DefaultSeverity);

    private static string TitleOf(DisciplinaryMeasureType type, Employee employee) => $"Medida – {RecordLabels.Of(type)} – {employee.EmployeeNumber}";

    private static string NewCaseNumber(DateTimeOffset now) => $"MED-{now.ToOffset(BusinessCalendar.Offset):yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private async Task<DisciplinaryCase> LoadAsync(Guid recordId, CancellationToken cancellationToken)
    {
        // Escopo: quem não enxerga a medida recebe 404, como na leitura.
        if (await _queries.GetByIdAsync(recordId, cancellationToken) is null)
            throw new KeyNotFoundException("Medida não encontrada.");

        var record = await _cases.GetByIdWithDetailsAsync(recordId, cancellationToken);
        if (record is null || record.Kind != DisciplinaryCaseKind.Record)
            throw new KeyNotFoundException("Medida não encontrada.");
        return record;
    }

    private async Task<RecordDetailDto> DetailAsync(Guid id, CancellationToken cancellationToken) =>
        await _queries.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Medida não encontrada.");
}
