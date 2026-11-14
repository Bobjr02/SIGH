using SIGH.Domain.Common;

namespace SIGH.Domain.Disciplinary.Entities;

public enum HistoryImportBatchStatus
{
    /// <summary>Pré-visualização feita; nada foi gravado.</summary>
    Previewed = 1,
    /// <summary>Confirmada e aplicada.</summary>
    Applied = 2
}

public sealed record HistoryImportCounts(
    int TotalRows, int Imported, int Updated, int Existing, int Rejected, int Warnings, int NewInfractionTypes, int Absent);

/// <summary>
/// Lote de importação do histórico disciplinar (planilha de penalidades). Guarda só metadados: SHA-256 e nome
/// do arquivo, quem e quando, contagens e avisos/rejeições (linha + matrícula + mensagem). O arquivo original
/// NÃO é guardado. A confirmação reenvia o mesmo arquivo (mesmo hash) antes de ExpiresAt.
/// </summary>
public class DisciplinaryHistoryImportBatch : Entity
{
    public const int MaxFileNameLength = 255;

    public string FileName { get; private set; } = string.Empty;
    public string FileSha256 { get; private set; } = string.Empty;
    public HistoryImportBatchStatus Status { get; private set; } = HistoryImportBatchStatus.Previewed;
    public Guid PreviewedByUserId { get; private set; }
    public DateTimeOffset PreviewedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? AppliedByUserId { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }

    public int TotalRows { get; private set; }
    public int ImportedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int ExistingCount { get; private set; }
    public int RejectedCount { get; private set; }
    public int WarningCount { get; private set; }
    public int NewInfractionTypeCount { get; private set; }
    public int AbsentCount { get; private set; }

    /// <summary>Impressão digital do plano da pré-visualização (detecta mudança da base antes da confirmação).</summary>
    public string PlanFingerprint { get; private set; } = string.Empty;

    /// <summary>JSON com avisos e rejeições: [{ row, employeeNumber, severity, message }].</summary>
    public string? IssuesJson { get; private set; }

    protected DisciplinaryHistoryImportBatch() { }

    public static DisciplinaryHistoryImportBatch CreatePreview(
        string fileName, string fileSha256, Guid userId, DateTimeOffset now, TimeSpan validity, HistoryImportCounts counts, string planFingerprint, string? issuesJson)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (name.Length > MaxFileNameLength) name = name[..MaxFileNameLength];

        var batch = new DisciplinaryHistoryImportBatch
        {
            FileName = string.IsNullOrEmpty(name) ? "historico.xlsx" : name,
            FileSha256 = fileSha256.ToLowerInvariant(),
            PreviewedByUserId = userId,
            PreviewedAt = now,
            ExpiresAt = now + validity,
            PlanFingerprint = planFingerprint,
            IssuesJson = issuesJson
        };
        batch.SetCounts(counts);
        return batch;
    }

    public bool IsExpired(DateTimeOffset now) => now > ExpiresAt;

    public void MarkApplied(Guid userId, DateTimeOffset now, HistoryImportCounts counts)
    {
        if (Status == HistoryImportBatchStatus.Applied)
            throw new InvalidOperationException("Lote já aplicado.");
        Status = HistoryImportBatchStatus.Applied;
        AppliedByUserId = userId;
        AppliedAt = now;
        SetCounts(counts);
    }

    private void SetCounts(HistoryImportCounts c)
    {
        TotalRows = c.TotalRows;
        ImportedCount = c.Imported;
        UpdatedCount = c.Updated;
        ExistingCount = c.Existing;
        RejectedCount = c.Rejected;
        WarningCount = c.Warnings;
        NewInfractionTypeCount = c.NewInfractionTypes;
        AbsentCount = c.Absent;
    }
}

/// <summary>
/// Estado da importação do histórico. Encerrada pela tela (sem volta pela tela); reabrir só por configuração
/// (History:ImportClosed), aplicada e auditada no startup. Linha única (Id fixo). Entidade auditada.
/// </summary>
public class HistoryImportState : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("4a1b0000-0000-4000-8000-000000000001");

    public bool IsClosed { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    protected HistoryImportState() : base() { }

    private HistoryImportState(Guid id) : base(id) { }

    public static HistoryImportState CreateOpen() => new(SingletonId);

    public void Close(Guid? userId, DateTimeOffset now)
    {
        if (IsClosed)
            throw new InvalidOperationException("A importação de histórico já está encerrada.");
        IsClosed = true;
        ClosedAt = now;
        ClosedByUserId = userId;
    }

    /// <summary>Só pela configuração History:ImportClosed=false (não há reabertura pela tela).</summary>
    public void ReopenByConfiguration()
    {
        IsClosed = false;
        ClosedAt = null;
        ClosedByUserId = null;
    }
}
