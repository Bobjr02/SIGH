using SIGH.Domain.Common;

namespace SIGH.Domain.Employees.Entities;

public enum EmployeeImportBatchStatus
{
    /// <summary>Pré-visualização feita; nada foi gravado em funcionários.</summary>
    Previewed = 1,
    /// <summary>Confirmada e aplicada.</summary>
    Applied = 2
}

/// <summary>
/// Lote de importação da base de funcionários (exportação do SAP). Guarda só metadados: hash SHA-256,
/// nome do arquivo, quem e quando, contagens e erros (linha + matrícula + mensagem, nunca CPF nem
/// colunas sensíveis). O arquivo original NÃO é guardado.
/// A confirmação referencia este lote: o mesmo arquivo (mesmo hash) precisa ser reenviado antes de ExpiresAt.
/// </summary>
public class EmployeeImportBatch : Entity
{
    public const int MaxFileNameLength = 255;

    public string FileName { get; private set; } = string.Empty;
    public string FileSha256 { get; private set; } = string.Empty;
    public EmployeeImportBatchStatus Status { get; private set; } = EmployeeImportBatchStatus.Previewed;
    public Guid PreviewedByUserId { get; private set; }
    public DateTimeOffset PreviewedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? AppliedByUserId { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }

    public int TotalRows { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int TerminatedCount { get; private set; }
    public int TerminatedByAbsenceCount { get; private set; }
    public int ReactivatedCount { get; private set; }
    public int UnchangedCount { get; private set; }
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public bool MassTerminationConfirmed { get; private set; }

    /// <summary>Impressão digital do plano calculado na pré-visualização (para detectar mudança da base).</summary>
    public string PlanFingerprint { get; private set; } = string.Empty;

    /// <summary>JSON com erros e avisos: [{ row, employeeNumber, message }]. Sem CPF nem colunas sensíveis.</summary>
    public string? IssuesJson { get; private set; }

    protected EmployeeImportBatch() { }

    public static EmployeeImportBatch CreatePreview(
        string fileName, string fileSha256, Guid userId, DateTimeOffset now, TimeSpan validity, EmployeeImportCounts counts,
        string planFingerprint, string? issuesJson)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (name.Length > MaxFileNameLength) name = name[..MaxFileNameLength];

        var batch = new EmployeeImportBatch
        {
            FileName = string.IsNullOrEmpty(name) ? "importacao.xlsx" : name,
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

    public void MarkApplied(Guid userId, DateTimeOffset now, EmployeeImportCounts counts, bool massTerminationConfirmed)
    {
        if (Status == EmployeeImportBatchStatus.Applied)
            throw new InvalidOperationException("Lote já aplicado.");
        Status = EmployeeImportBatchStatus.Applied;
        AppliedByUserId = userId;
        AppliedAt = now;
        MassTerminationConfirmed = massTerminationConfirmed;
        SetCounts(counts);
    }

    private void SetCounts(EmployeeImportCounts c)
    {
        TotalRows = c.TotalRows;
        CreatedCount = c.Created;
        UpdatedCount = c.Updated;
        TerminatedCount = c.Terminated;
        TerminatedByAbsenceCount = c.TerminatedByAbsence;
        ReactivatedCount = c.Reactivated;
        UnchangedCount = c.Unchanged;
        ErrorCount = c.Errors;
        WarningCount = c.Warnings;
    }
}

public sealed record EmployeeImportCounts(
    int TotalRows, int Created, int Updated, int Terminated, int TerminatedByAbsence, int Reactivated, int Unchanged, int Errors, int Warnings);
