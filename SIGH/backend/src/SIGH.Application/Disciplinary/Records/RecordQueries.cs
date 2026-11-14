using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Records;

public interface IRecordQueries
{
    Task<PagedResult<RecordSummaryDto>> GetPagedAsync(GetRecordsQuery query, CancellationToken cancellationToken = default);

    /// <returns>null se não existe ou está fora do escopo (o controller responde 404).</returns>
    Task<RecordDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecordOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Medidas aguardando aprovação que o usuário pode aprovar (contador do menu).</summary>
    Task<int> CountPendingForApprovalAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Leitura das medidas (processos do tipo Record). Escopo aplicado no banco: o gestor só vê medidas de
/// funcionários que pertencem HOJE às gerências dele; a paginação é feita no SQL.
/// </summary>
public sealed class RecordQueries : IRecordQueries
{
    private readonly IApplicationDbContext _db;
    private readonly IAccessScope _scope;
    private readonly ICurrentUserService _currentUser;

    public RecordQueries(IApplicationDbContext db, IAccessScope scope, ICurrentUserService currentUser)
    {
        _db = db;
        _scope = scope;
        _currentUser = currentUser;
    }

    /// <summary>Projeção com inicializador de membros (o EF compõe filtros e ordenação sobre ela no SQL).</summary>
    private sealed class Row
    {
        public Guid Id { get; init; }
        public string CaseNumber { get; init; } = string.Empty;
        public DisciplinaryCaseStatus CaseStatus { get; init; }
        public bool HasPendingDecision { get; init; }
        public Guid EmployeeId { get; init; }
        public string EmployeeNumber { get; init; } = string.Empty;
        public string EmployeeName { get; init; } = string.Empty;
        public Guid EmployeeUnitId { get; init; }
        public Guid? EmployeeDepartmentId { get; init; }
        public string? UnitCode { get; init; }
        public string? UnitName { get; init; }
        public string? DepartmentName { get; init; }
        public DisciplinaryMeasureType? MeasureType { get; init; }
        public DateOnly? MeasureDate { get; init; }
        public DateOnly? SuspensionStart { get; init; }
        public DateOnly? SuspensionEnd { get; init; }
        public Guid? InfractionTypeId { get; init; }
        public string? InfractionTypeName { get; init; }
        public DateTimeOffset? FirstOccurrence { get; init; }
        public DateTimeOffset OpenedAt { get; init; }
        public Guid OpenedBy { get; init; }
        public string? OpenedByName { get; init; }
        public DisciplinaryCaseKind Kind { get; init; }
        public DateTimeOffset? MeasureEffectiveFrom { get; init; }
        public DateTimeOffset? MeasureEffectiveUntil { get; init; }
        public int? MeasureSuspensionDays { get; init; }
        public DateTimeOffset? ImportedAt { get; init; }
    }

    /// <summary>Medidas visíveis, já com o escopo.</summary>
    private async Task<IQueryable<DisciplinaryCase>> VisibleRecordsAsync(CancellationToken cancellationToken)
    {
        var scope = await _scope.GetFilterAsync(cancellationToken);
        return _db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.Kind == DisciplinaryCaseKind.Record || c.Kind == DisciplinaryCaseKind.Historical)
            .InScope(_db.DisciplinaryCaseEmployees, _db.Employees, scope);
    }

    private IQueryable<Row> Project(IQueryable<DisciplinaryCase> cases) =>
        from c in cases
        let accusedId = c.Employees.Where(e => e.Role == CaseEmployeeRole.Accused).Select(e => e.EmployeeId).FirstOrDefault()
        let d = c.Decisions.OrderByDescending(x => x.DecidedAt).FirstOrDefault()
        let infractionId = c.Occurrences.Where(o => o.Status != OccurrenceStatus.Rejected).Select(o => (Guid?)o.InfractionTypeId).FirstOrDefault()
        let m = c.Measures.Where(x => x.Status != DisciplinaryMeasureStatus.Cancelled).FirstOrDefault()
        join emp in _db.Employees on accusedId equals emp.Id
        join unit in _db.ManagementUnits on emp.ManagementUnitId equals unit.Id into units
        from unit in units.DefaultIfEmpty()
        join dept in _db.Departments on emp.DepartmentId equals dept.Id into depts
        from dept in depts.DefaultIfEmpty()
        join user in _db.Users on c.OpenedByUserId equals user.Id into users
        from user in users.DefaultIfEmpty()
        select new Row
        {
            Id = c.Id,
            CaseNumber = c.CaseNumber,
            CaseStatus = c.Status,
            HasPendingDecision = c.Decisions.Any(x => x.Status == DecisionStatus.PendingApproval),
            EmployeeId = emp.Id,
            EmployeeNumber = emp.EmployeeNumber,
            EmployeeName = emp.FullName,
            EmployeeUnitId = emp.ManagementUnitId,
            EmployeeDepartmentId = emp.DepartmentId,
            UnitCode = unit.Code,
            UnitName = unit.Name,
            DepartmentName = dept.Name,
            // Histórico não tem decisão: o tipo vem da medida.
            MeasureType = d!.ProposedMeasureType ?? (DisciplinaryMeasureType?)m!.MeasureType,
            MeasureDate = d.MeasureDate,
            SuspensionStart = d.SuspensionStart,
            SuspensionEnd = d.SuspensionEnd,
            InfractionTypeId = infractionId,
            InfractionTypeName = _db.InfractionTypes.Where(i => i.Id == infractionId).Select(i => i.Name).FirstOrDefault(),
            FirstOccurrence = c.Occurrences.Where(o => o.Status != OccurrenceStatus.Rejected).Min(o => (DateTimeOffset?)o.OccurrenceDate),
            OpenedAt = c.OpenedAt,
            OpenedBy = c.OpenedByUserId,
            OpenedByName = user.FullName,
            Kind = c.Kind,
            MeasureEffectiveFrom = (DateTimeOffset?)m!.EffectiveFrom,
            MeasureEffectiveUntil = m.EffectiveUntil,
            MeasureSuspensionDays = m.SuspensionDays,
            ImportedAt = _db.DisciplinaryHistoryImportBatches.Where(b => b.Id == c.HistoryImportBatchId).Select(b => b.AppliedAt).FirstOrDefault(),
        };

    public async Task<PagedResult<RecordSummaryDto>> GetPagedAsync(GetRecordsQuery query, CancellationToken cancellationToken = default)
    {
        var cases = await VisibleRecordsAsync(cancellationToken);

        cases = query.Status switch
        {
            RecordStatus.PendingApproval => cases.Where(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision && c.Decisions.Any(d => d.Status == DecisionStatus.PendingApproval)),
            RecordStatus.Rejected => cases.Where(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision && !c.Decisions.Any(d => d.Status == DecisionStatus.PendingApproval)),
            RecordStatus.Approved => cases.Where(c => c.Kind == DisciplinaryCaseKind.Record && c.Status == DisciplinaryCaseStatus.Completed),
            RecordStatus.Historical => cases.Where(c => c.Kind == DisciplinaryCaseKind.Historical),
            RecordStatus.Cancelled => cases.Where(c => c.Status == DisciplinaryCaseStatus.Cancelled),
            _ => cases
        };

        cases = query.Origin switch
        {
            RecordOrigin.Sigh => cases.Where(c => c.Kind == DisciplinaryCaseKind.Record),
            RecordOrigin.Historical => cases.Where(c => c.Kind == DisciplinaryCaseKind.Historical),
            _ => cases
        };

        if (query.Mine && _currentUser.UserId is { } me)
            cases = cases.Where(c => c.OpenedByUserId == me);

        if (query.InfractionTypeId is { } infraction)
            cases = cases.Where(c => c.Occurrences.Any(o => o.InfractionTypeId == infraction && o.Status != OccurrenceStatus.Rejected));

        var rows = Project(cases);

        if (query.MeasureType is { } type)
            rows = rows.Where(r => r.MeasureType == type);
        if (query.ManagementUnitId is { } unitId)
            rows = rows.Where(r => r.EmployeeUnitId == unitId);
        if (query.DepartmentId is { } departmentId)
            rows = rows.Where(r => r.EmployeeDepartmentId == departmentId);
        // Período pela data da medida; no histórico (sem decisão) é a data da ocorrência (= DATA da planilha).
        if (query.From is { } from)
        {
            var fromInstant = BusinessCalendar.StartOf(from);
            rows = rows.Where(r => r.MeasureDate != null ? r.MeasureDate >= from : r.FirstOccurrence >= fromInstant);
        }
        if (query.To is { } to)
        {
            var toInstant = BusinessCalendar.StartOf(to.AddDays(1));
            rows = rows.Where(r => r.MeasureDate != null ? r.MeasureDate <= to : r.FirstOccurrence < toInstant);
        }
        if (!string.IsNullOrWhiteSpace(query.Employee))
        {
            var term = query.Employee.Trim();
            rows = rows.Where(r => r.EmployeeNumber.StartsWith(term) || r.EmployeeName.Contains(term));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var pageNumber = Math.Max(1, query.PageNumber);
        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(r => r.OpenedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RecordSummaryDto>(page.Select(ToSummary).ToList(), pageNumber, pageSize, total);
    }

    public async Task<RecordDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cases = (await VisibleRecordsAsync(cancellationToken)).Where(c => c.Id == id);
        var row = await Project(cases).FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var entity = await _db.DisciplinaryCases.AsNoTracking()
            .Include(c => c.Occurrences)
            .Include(c => c.Decisions)
            .Include(c => c.Evidences)
            .Include(c => c.Employees)
            .SingleAsync(c => c.Id == id, cancellationToken);

        var userIds = entity.Decisions
            .SelectMany(d => new[] { (Guid?)d.DecidedByUserId, d.ApprovedByUserId, d.RejectedByUserId })
            .Where(u => u is not null).Select(u => u!.Value).Distinct().ToList();
        var names = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
        string? NameOf(Guid? userId) => userId is { } x && names.TryGetValue(x, out var n) ? n : null;

        var history = entity.Decisions
            .OrderBy(d => d.DecidedAt)
            .Select(d => new RecordDecisionHistoryDto(d.Id, d.Status, d.ProposedMeasureType, d.DecidedByUserId, NameOf(d.DecidedByUserId), d.DecidedAt,
                d.ApprovedByUserId, NameOf(d.ApprovedByUserId), d.ApprovedAt, d.RejectedByUserId, NameOf(d.RejectedByUserId), d.RejectedAt, d.RejectionReason))
            .ToList();
        var approved = entity.Decisions.FirstOrDefault(d => d.Status == DecisionStatus.Approved);
        var lastRejected = entity.Decisions.Where(d => d.Status == DecisionStatus.Rejected).OrderByDescending(d => d.RejectedAt).FirstOrDefault();

        var accused = entity.Employees.FirstOrDefault(e => e.Role == CaseEmployeeRole.Accused);
        string? unitAtTime = null;
        if (accused?.ManagementUnitIdAtTime is { } unitAtTimeId)
            unitAtTime = await _db.ManagementUnits.AsNoTracking().Where(m => m.Id == unitAtTimeId).Select(m => m.Code).FirstOrDefaultAsync(cancellationToken);

        var summary = ToSummary(row);
        var me = _currentUser.UserId;
        var pending = summary.Status == RecordStatus.PendingApproval;
        var rejected = summary.Status == RecordStatus.Rejected;
        var currentDecider = entity.Decision?.DecidedByUserId;
        var canReview = pending && _currentUser.HasPermission(PermissionCodes.DisciplinaryRecords.Approve)
                        && me != entity.OpenedByUserId && me != currentDecider;
        var canRegister = _currentUser.HasPermission(PermissionCodes.DisciplinaryRecords.Register);
        // Anexos: pendente, aprovada ou histórico (carta assinada antiga); nunca cancelada.
        var attachable = summary.Status is RecordStatus.PendingApproval or RecordStatus.Approved or RecordStatus.Historical;

        return new RecordDetailDto(
            summary,
            entity.Description,
            entity.Occurrences.Where(o => o.Status != OccurrenceStatus.Rejected)
                .Select(o => BusinessCalendar.DateOf(o.OccurrenceDate)).Distinct().Order().ToList(),
            PeriodOf(row).Start,
            PeriodOf(row).End,
            unitAtTime,
            approved?.ApprovedAt,
            NameOf(approved?.ApprovedByUserId),
            entity.CancelledAt,
            entity.CancellationReason,
            lastRejected?.RejectionReason,
            NameOf(lastRejected?.RejectedByUserId),
            lastRejected?.RejectedAt,
            entity.Evidences.Where(e => !e.IsDeleted)
                .OrderBy(e => e.CollectedAt)
                .Select(e => new RecordEvidenceDto(e.Id, e.Title, e.Description, e.EvidenceType.ToString(), e.HasFile, e.OriginalFileName,
                    e.ContentType, e.FileSize, e.CollectedAt, e.CollectedByUserId))
                .ToList(),
            history,
            new RecordActionsDto(
                CanApprove: canReview,
                CanReject: canReview,
                CanCorrect: rejected && canRegister,
                CanCancel: rejected && canRegister,
                CanAttach: attachable && _currentUser.HasPermission(PermissionCodes.DisciplinaryCases.AddEvidence),
                CanRemoveEvidence: attachable && _currentUser.HasPermission(PermissionCodes.DisciplinaryCases.RemoveEvidence)));
    }

    public async Task<RecordOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var scope = await _scope.GetFilterAsync(cancellationToken);

        var infractionTypes = await _db.InfractionTypes.AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new OptionDto(i.Id, i.Code, i.Name))
            .ToListAsync(cancellationToken);

        // Máscara do escopo: o gestor só vê as próprias gerências (e os setores delas) nos filtros.
        var unitIds = scope.ManagementUnitIds.ToList();
        var units = await _db.ManagementUnits.AsNoTracking()
            .Where(m => scope.All || unitIds.Contains(m.Id))
            .OrderBy(m => m.Code)
            .Select(m => new OptionDto(m.Id, m.Code, m.Name))
            .ToListAsync(cancellationToken);

        var departments = await _db.Departments.AsNoTracking()
            .Where(d => scope.All || unitIds.Contains(d.ManagementUnitId))
            .OrderBy(d => d.Name)
            .Select(d => new OptionDto(d.Id, d.Code, d.Name))
            .ToListAsync(cancellationToken);

        return new RecordOptionsDto(
            RecordLabels.RegistrableTypes.Select(t => new MeasureTypeOptionDto(t, RecordLabels.Of(t))).ToList(),
            infractionTypes,
            units,
            departments);
    }

    public async Task<int> CountPendingForApprovalAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasPermission(PermissionCodes.DisciplinaryRecords.Approve) || _currentUser.UserId is not { } me)
            return 0;

        var cases = await VisibleRecordsAsync(cancellationToken);
        return await cases.CountAsync(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision
                                           && c.OpenedByUserId != me
                                           && c.Decisions.Any(d => d.Status == DecisionStatus.PendingApproval && d.DecidedByUserId != me),
            cancellationToken);
    }

    private static RecordStatus StatusOf(Row row) => row.Kind == DisciplinaryCaseKind.Historical ? RecordStatus.Historical : row.CaseStatus switch
    {
        DisciplinaryCaseStatus.Completed => RecordStatus.Approved,
        DisciplinaryCaseStatus.Cancelled => RecordStatus.Cancelled,
        _ => row.HasPendingDecision ? RecordStatus.PendingApproval : RecordStatus.Rejected
    };

    private static RecordSummaryDto ToSummary(Row row)
    {
        var status = StatusOf(row);
        var type = row.MeasureType ?? DisciplinaryMeasureType.Other;
        var historical = row.Kind == DisciplinaryCaseKind.Historical;
        var (start, end) = PeriodOf(row);
        int? days = row.MeasureSuspensionDays ?? (start is { } s && end is { } e ? e.DayNumber - s.DayNumber + 1 : null);
        var firstOccurrence = row.FirstOccurrence is { } f ? BusinessCalendar.DateOf(f) : (DateOnly?)null;
        return new RecordSummaryDto(
            row.Id, row.CaseNumber, status, RecordLabels.Of(status),
            row.EmployeeId, row.EmployeeNumber, row.EmployeeName, row.UnitCode, row.UnitName, row.DepartmentName,
            type, RecordLabels.Of(type), row.InfractionTypeId, row.InfractionTypeName,
            historical ? firstOccurrence : row.MeasureDate, firstOccurrence, type == DisciplinaryMeasureType.Suspension ? days : null,
            row.OpenedAt, row.OpenedBy, row.OpenedByName,
            historical ? RecordOrigin.Historical : RecordOrigin.Sigh,
            historical ? "Histórico" : "SIGH",
            historical ? row.ImportedAt : null,
            type == DisciplinaryMeasureType.Suspension && days is null);
    }

    /// <summary>Período da suspensão: da decisão (SIGH) ou da própria medida (histórico, quando houver).</summary>
    private static (DateOnly? Start, DateOnly? End) PeriodOf(Row row)
    {
        if (row.Kind != DisciplinaryCaseKind.Historical)
            return (row.SuspensionStart, row.SuspensionEnd);
        return row.MeasureEffectiveUntil is { } until && row.MeasureEffectiveFrom is { } from
            ? (BusinessCalendar.DateOf(from), BusinessCalendar.DateOf(until))
            : (null, null);
    }
}
