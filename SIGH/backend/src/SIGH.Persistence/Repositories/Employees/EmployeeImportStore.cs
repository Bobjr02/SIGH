using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Import;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;

namespace SIGH.Persistence.Repositories.Employees;

public sealed class EmployeeImportStore : IEmployeeImportStore
{
    private readonly SighDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<EmployeeImportStore> _logger;

    public EmployeeImportStore(SighDbContext context, Microsoft.Extensions.Logging.ILogger<EmployeeImportStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Instalação de empresa única: tudo vai para a empresa padrão.</summary>
    public Guid CompanyId => DatabaseSeeder.DefaultCompanyId;

    public async Task<(IReadOnlyList<EmployeeSnapshot> Employees, ReferenceSnapshot References)> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var companyId = CompanyId;
        var jobs = await _context.JobTitles.AsNoTracking().Where(j => j.CompanyId == companyId && j.Code != null)
            .Select(j => new { j.Id, Code = j.Code!, j.Name }).ToListAsync(cancellationToken);
        var depts = await _context.Departments.AsNoTracking().Where(d => d.CompanyId == companyId && d.Code != null)
            .Select(d => new { d.Id, Code = d.Code!, d.Name }).ToListAsync(cancellationToken);
        var mus = await _context.ManagementUnits.AsNoTracking().Where(m => m.CompanyId == companyId && m.Code != null)
            .Select(m => new { m.Id, Code = m.Code! }).ToListAsync(cancellationToken);

        var jobCode = jobs.ToDictionary(j => j.Id, j => j.Code);
        var deptCode = depts.ToDictionary(d => d.Id, d => d.Code);
        var muCode = mus.ToDictionary(m => m.Id, m => m.Code);

        var employees = await _context.Employees.AsNoTracking().Where(e => e.CompanyId == companyId)
            .Select(e => new
            {
                e.Id, e.EmployeeNumber, e.FullName, e.Cpf, e.AdmissionDate, e.TerminationDate, e.Status,
                e.JobTitleId, e.DepartmentId, e.ManagementUnitId, e.Group, e.Team
            })
            .ToListAsync(cancellationToken);

        var snapshots = employees.Select(e => new EmployeeSnapshot(
            e.Id, e.EmployeeNumber, e.FullName, e.Cpf, e.AdmissionDate, e.TerminationDate, e.Status,
            jobCode.GetValueOrDefault(e.JobTitleId),
            e.DepartmentId is { } d ? deptCode.GetValueOrDefault(d) : null,
            muCode.GetValueOrDefault(e.ManagementUnitId),
            e.Group, e.Team)).ToList();

        var references = new ReferenceSnapshot(
            jobs.GroupBy(j => j.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase),
            depts.GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase),
            mus.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase));

        return (snapshots, references);
    }

    public Task<EmployeeImportBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken) =>
        _context.EmployeeImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

    public async Task AddBatchAsync(EmployeeImportBatch batch, CancellationToken cancellationToken)
    {
        _context.EmployeeImportBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyAsync(EmployeeImportPlan plan, EmployeeImportBatch batch, DateOnly importDate, Guid actorUserId, bool massTerminationConfirmed, CancellationToken cancellationToken)
    {
        var companyId = CompanyId;
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // 1. Dados de referência (gerências, cargos, setores).
            var mus = await _context.ManagementUnits.Where(m => m.CompanyId == companyId && m.Code != null).ToListAsync(cancellationToken);
            foreach (var code in plan.NewManagementUnits)
            {
                var mu = ManagementUnit.Create(companyId, code, code);
                _context.ManagementUnits.Add(mu);
                mus.Add(mu);
            }
            var muByCode = mus.GroupBy(m => m.Code!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var jobs = await _context.JobTitles.Where(j => j.CompanyId == companyId && j.Code != null).ToListAsync(cancellationToken);
            foreach (var (code, name) in plan.NewJobTitles)
            {
                var job = JobTitle.Create(companyId, name, code);
                _context.JobTitles.Add(job);
                jobs.Add(job);
            }
            foreach (var (code, name) in plan.RenamedJobTitles)
                jobs.First(j => string.Equals(j.Code, code, StringComparison.OrdinalIgnoreCase)).UpdateName(name);
            var jobByCode = jobs.GroupBy(j => j.Code!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var depts = await _context.Departments.Where(d => d.CompanyId == companyId && d.Code != null).ToListAsync(cancellationToken);
            foreach (var (code, name, mu) in plan.NewDepartments)
            {
                var dept = Department.Create(companyId, muByCode[mu], name, code);
                _context.Departments.Add(dept);
                depts.Add(dept);
            }
            foreach (var (code, name) in plan.RenamedDepartments)
                depts.First(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase)).UpdateName(name);
            var deptByCode = depts.GroupBy(d => d.Code!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            await _context.SaveChangesAsync(cancellationToken);
            var referencesMs = watch.ElapsedMilliseconds;

            HrRecord Record(HrRow r) => new(
                r.FullName, r.Cpf, r.AdmissionDate, jobByCode[r.JobTitleCode], muByCode[r.ManagementUnitCode], deptByCode[r.DepartmentCode],
                r.Group, r.Team, r.Status, r.Status == SIGH.Domain.Employees.Enums.EmployeeStatus.Terminated ? r.TerminationDate : null,
                r.Status == SIGH.Domain.Employees.Enums.EmployeeStatus.Terminated ? $"Desligamento informado pelo SAP (lote {batch.Id})." : null);

            // 2. Alterações e desligamentos antes das criações: numa readmissão, a matrícula antiga é
            // desligada antes de a nova (mesmo CPF) entrar, respeitando o índice único de CPF.
            var existingIds = plan.Changes.Where(c => c.EmployeeId.HasValue).Select(c => c.EmployeeId!.Value).ToList();
            var tracked = new Dictionary<Guid, Employee>();
            foreach (var chunk in existingIds.Chunk(1000))
            {
                var loaded = await _context.Employees.Where(e => chunk.Contains(e.Id)).ToListAsync(cancellationToken);
                foreach (var e in loaded) tracked[e.Id] = e;
            }

            var absenceReason = $"{EmployeeImportPlanner.AbsenceReasonPrefix} importada em {importDate:dd/MM/yyyy} (lote {batch.Id}).";
            foreach (var change in plan.Changes.Where(c => c.Kind == EmployeeChangeKind.TerminatedByAbsence))
                tracked[change.EmployeeId!.Value].MarkAbsentFromHrSystem(importDate, absenceReason);
            foreach (var change in plan.Changes.Where(c => c.Kind is EmployeeChangeKind.Updated or EmployeeChangeKind.Terminated))
                tracked[change.EmployeeId!.Value].ApplyHrRecord(Record(change.Row!));
            await _context.SaveChangesAsync(cancellationToken);

            // Reativações depois dos desligamentos (podem reutilizar um CPF liberado neste mesmo lote).
            foreach (var change in plan.Changes.Where(c => c.Kind == EmployeeChangeKind.Reactivated))
                tracked[change.EmployeeId!.Value].ApplyHrRecord(Record(change.Row!));
            await _context.SaveChangesAsync(cancellationToken);

            var updatesMs = watch.ElapsedMilliseconds;

            // 3. Criações.
            foreach (var change in plan.Changes.Where(c => c.Kind == EmployeeChangeKind.Created))
                _context.Employees.Add(Employee.CreateFromHrSystem(companyId, change.EmployeeNumber, Record(change.Row!)));
            await _context.SaveChangesAsync(cancellationToken);

            batch.MarkApplied(actorUserId, DateTimeOffset.UtcNow, new EmployeeImportCounts(
                plan.TotalRows,
                plan.Of(EmployeeChangeKind.Created).Count(),
                plan.Of(EmployeeChangeKind.Updated).Count(),
                plan.Of(EmployeeChangeKind.Terminated).Count(),
                plan.Of(EmployeeChangeKind.TerminatedByAbsence).Count(),
                plan.Of(EmployeeChangeKind.Reactivated).Count(),
                plan.UnchangedEmployeeNumbers.Count,
                plan.ErrorCount,
                plan.WarningCount), massTerminationConfirmed);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Só contagens e tempos: nunca dados de funcionários.
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger,
                "Importação {BatchId} aplicada em {TotalMs} ms (referências {RefMs} ms, alterações {UpdMs} ms, criações e lote {CreateMs} ms).",
                batch.Id, watch.ElapsedMilliseconds, referencesMs, updatesMs - referencesMs, watch.ElapsedMilliseconds - updatesMs);
        });
    }

    public async Task<PagedResult<EmployeeImportBatchDto>> ListBatchesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.EmployeeImportBatches.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderByDescending(b => b.PreviewedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(b => new
            {
                Batch = b,
                PreviewedBy = _context.Users.IgnoreQueryFilters().Where(u => u.Id == b.PreviewedByUserId).Select(u => u.FullName).FirstOrDefault(),
                AppliedBy = _context.Users.IgnoreQueryFilters().Where(u => u.Id == b.AppliedByUserId).Select(u => u.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new EmployeeImportBatchDto(
            x.Batch.Id, x.Batch.FileName, x.Batch.FileSha256, x.Batch.Status == EmployeeImportBatchStatus.Applied ? "Applied" : "Previewed",
            x.Batch.PreviewedAt, x.PreviewedBy, x.Batch.AppliedAt, x.AppliedBy,
            new ImportSummaryDto(x.Batch.TotalRows, x.Batch.CreatedCount, x.Batch.UpdatedCount, x.Batch.TerminatedCount,
                x.Batch.TerminatedByAbsenceCount, x.Batch.ReactivatedCount, x.Batch.UnchangedCount, x.Batch.ErrorCount, x.Batch.WarningCount),
            x.Batch.MassTerminationConfirmed)).ToList();

        return new PagedResult<EmployeeImportBatchDto>(dtos, pageNumber, pageSize, total);
    }
}
