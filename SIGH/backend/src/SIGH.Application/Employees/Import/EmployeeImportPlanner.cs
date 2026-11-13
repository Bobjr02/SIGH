using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SIGH.Domain.Employees.Enums;

namespace SIGH.Application.Employees.Import;

/// <summary>Estado atual de um funcionário, como o planejador precisa (códigos em vez de ids).</summary>
public sealed record EmployeeSnapshot(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Cpf,
    DateOnly AdmissionDate,
    DateOnly? TerminationDate,
    EmployeeStatus Status,
    string? JobTitleCode,
    string? DepartmentCode,
    string? ManagementUnitCode,
    EmployeeGroup? Group,
    string? Team);

public sealed record ReferenceSnapshot(
    IReadOnlyDictionary<string, string> JobTitles,          // código → descrição
    IReadOnlyDictionary<string, string> Departments,        // código → descrição
    IReadOnlySet<string> ManagementUnits);                  // códigos

public enum EmployeeChangeKind
{
    Created = 1,
    Updated = 2,
    Terminated = 3,
    TerminatedByAbsence = 4,
    Reactivated = 5
}

public sealed record FieldChange(string Field, string? OldValue, string? NewValue);

public sealed record PlannedChange(
    EmployeeChangeKind Kind,
    string EmployeeNumber,
    string FullName,
    Guid? EmployeeId,
    HrRow? Row,
    IReadOnlyList<FieldChange> Changes);

public sealed record EmployeeImportPlan(
    int TotalRows,
    IReadOnlyList<PlannedChange> Changes,
    IReadOnlyList<string> UnchangedEmployeeNumbers,
    IReadOnlyList<HrImportIssue> Issues,
    IReadOnlyList<(string Code, string Name)> NewJobTitles,
    IReadOnlyList<(string Code, string Name)> RenamedJobTitles,
    IReadOnlyList<(string Code, string Name, string ManagementUnitCode)> NewDepartments,
    IReadOnlyList<(string Code, string Name)> RenamedDepartments,
    IReadOnlyList<string> NewManagementUnits,
    int ActiveBefore,
    double AbsenceTerminationPercent,
    bool RequiresMassTerminationConfirmation,
    string Fingerprint)
{
    public IEnumerable<PlannedChange> Of(EmployeeChangeKind kind) => Changes.Where(c => c.Kind == kind);
    public int ErrorCount => Issues.Count(i => i.Severity == HrIssueSeverity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == HrIssueSeverity.Warning);
}

/// <summary>
/// Regras de sincronização com o SAP:
/// - matrícula nova: cria (inclusive já desligada, se assim vier);
/// - matrícula existente com dados diferentes: atualiza;
/// - status de desligado no arquivo: desliga;
/// - matrícula ativa ausente do arquivo: desliga com a data da importação (nunca apaga);
/// - desligada que volta ativa/afastada: reativa.
/// Linhas com erro são rejeitadas, mas a matrícula delas NÃO conta como ausente.
/// </summary>
public static class EmployeeImportPlanner
{
    public const string AbsenceReasonPrefix = "Ausente da exportação do SAP";

    public static EmployeeImportPlan Plan(
        IReadOnlyList<HrRawRow> rawRows,
        IReadOnlyList<string> sheetWarnings,
        IReadOnlyList<EmployeeSnapshot> existing,
        ReferenceSnapshot references,
        DateOnly importDate,
        double maxTerminationPercent)
    {
        var issues = sheetWarnings.Select(w => new HrImportIssue(null, null, HrIssueSeverity.Warning, w)).ToList();

        // 1. Validação por linha.
        var rows = new List<HrRow>();
        var presentNumbers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in rawRows)
        {
            var row = HrRowValidator.Validate(raw, issues);
            if (row is not null) rows.Add(row);
            else
            {
                // Linha rejeitada: se der para identificar a matrícula, ela não será tratada como ausente.
                var n = raw.Get(HrField.EmployeeNumber);
                var digits = new string(n.Text.Where(char.IsAsciiDigit).ToArray());
                if (n.WasNumber && digits.Length > 0) digits = digits.PadLeft(HrRowValidator.EmployeeNumberLength, '0');
                if (digits.Length == HrRowValidator.EmployeeNumberLength) presentNumbers.Add(digits);
            }
        }

        // Toda matrícula válida do arquivo está presente, mesmo que a linha seja rejeitada adiante por
        // duplicidade: linha rejeitada nunca conta como ausente (não desliga ninguém).
        foreach (var r in rows) presentNumbers.Add(r.EmployeeNumber);

        var byNumber = existing.ToDictionary(e => e.EmployeeNumber, StringComparer.Ordinal);

        // 2. Duplicidades dentro do arquivo.
        // Matrícula repetida: todas as ocorrências são rejeitadas.
        rows = RejectDuplicates(rows, r => r.EmployeeNumber, "Matrícula repetida no arquivo (linhas {0}).", issues, _ => true);
        // CPF repetido em mais de uma linha não desligada.
        rows = RejectDuplicateCpfs(rows, byNumber, issues);
        // Uma linha ativa mais linhas desligadas com o mesmo CPF é a readmissão no SAP (matrícula nova;
        // a exportação traz os desligados).
        foreach (var group in rows.GroupBy(r => r.Cpf).Where(g => g.Count() > 1))
        {
            var active = group.FirstOrDefault(r => r.Status != EmployeeStatus.Terminated);
            if (active is null) continue;
            var previous = string.Join(", ", group.Where(r => r != active).Select(r => r.EmployeeNumber));
            issues.Add(new HrImportIssue(active.RowNumber, active.EmployeeNumber, HrIssueSeverity.Warning,
                $"Possível readmissão: o CPF também aparece na(s) matrícula(s) desligada(s) {previous} deste arquivo."));
        }

        // 3. Conflitos de CPF com o banco: o CPF não pode ficar em duas matrículas não desligadas.
        rows = ResolveCpfConflicts(rows, existing, byNumber, presentNumbers, issues);

        // 4. Plano por linha.
        var changes = new List<PlannedChange>();
        var unchanged = new List<string>();
        foreach (var row in rows)
        {
            if (!byNumber.TryGetValue(row.EmployeeNumber, out var current))
            {
                changes.Add(new PlannedChange(EmployeeChangeKind.Created, row.EmployeeNumber, row.FullName, null, row, Describe(row)));
                continue;
            }

            var diff = Diff(current, row);
            if (diff.Count == 0)
            {
                unchanged.Add(row.EmployeeNumber);
                continue;
            }

            var kind = current.Status == EmployeeStatus.Terminated && row.Status != EmployeeStatus.Terminated
                ? EmployeeChangeKind.Reactivated
                : current.Status != EmployeeStatus.Terminated && row.Status == EmployeeStatus.Terminated
                    ? EmployeeChangeKind.Terminated
                    : EmployeeChangeKind.Updated;
            changes.Add(new PlannedChange(kind, row.EmployeeNumber, row.FullName, current.Id, row, diff));
        }

        // 5. Ausentes do arquivo: só quem não está desligado. Desligados ausentes ficam como estão.
        var importDateText = importDate.ToString(HrRowValidator.DateFormat, CultureInfo.InvariantCulture);
        foreach (var employee in existing.Where(e => e.Status != EmployeeStatus.Terminated && !presentNumbers.Contains(e.EmployeeNumber)))
        {
            changes.Add(new PlannedChange(EmployeeChangeKind.TerminatedByAbsence, employee.EmployeeNumber, employee.FullName, employee.Id, null,
            [
                new FieldChange("Situação", StatusLabel(employee.Status), StatusLabel(EmployeeStatus.Terminated)),
                new FieldChange("Data demissão", null, importDateText)
            ]));
        }

        // 6. Dados de referência que serão criados ou renomeados.
        var newJobs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var renamedJobs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var newDepts = new Dictionary<string, (string Name, string Mu)>(StringComparer.OrdinalIgnoreCase);
        var renamedDepts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var newMus = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        // Toda linha aceita está em "changes" ou em "unchanged".
        foreach (var row in rows)
        {
            if (!references.JobTitles.TryGetValue(row.JobTitleCode, out var jobName)) newJobs.TryAdd(row.JobTitleCode, row.JobTitleName);
            else if (jobName != row.JobTitleName) renamedJobs.TryAdd(row.JobTitleCode, row.JobTitleName);

            if (!references.Departments.TryGetValue(row.DepartmentCode, out var deptName)) newDepts.TryAdd(row.DepartmentCode, (row.DepartmentName, row.ManagementUnitCode));
            else if (deptName != row.DepartmentName) renamedDepts.TryAdd(row.DepartmentCode, row.DepartmentName);

            if (!references.ManagementUnits.Contains(row.ManagementUnitCode) && newMus.Add(row.ManagementUnitCode))
                issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Warning,
                    $"Chave Organização {row.ManagementUnitCode} desconhecida: a gerência será criada com o próprio código como nome."));
        }

        // 7. Proteção contra desligamento em massa.
        var activeBefore = existing.Count(e => e.Status != EmployeeStatus.Terminated);
        var absences = changes.Count(c => c.Kind == EmployeeChangeKind.TerminatedByAbsence);
        var percent = activeBefore == 0 ? 0 : 100.0 * absences / activeBefore;
        var requiresConfirmation = absences > 0 && percent > maxTerminationPercent;

        var ordered = changes.OrderBy(c => c.Kind).ThenBy(c => c.EmployeeNumber, StringComparer.Ordinal).ToList();
        var sortedIssues = issues.OrderBy(i => i.RowNumber ?? 0).ThenBy(i => i.Severity).ToList();

        return new EmployeeImportPlan(
            rawRows.Count, ordered, unchanged, sortedIssues,
            newJobs.Select(kv => (kv.Key, kv.Value)).ToList(),
            renamedJobs.Select(kv => (kv.Key, kv.Value)).ToList(),
            newDepts.Select(kv => (kv.Key, kv.Value.Name, kv.Value.Mu)).ToList(),
            renamedDepts.Select(kv => (kv.Key, kv.Value)).ToList(),
            newMus.ToList(),
            activeBefore, Math.Round(percent, 2), requiresConfirmation,
            Fingerprint(ordered, unchanged, sortedIssues));
    }

    /// <summary>
    /// CPF repetido em mais de uma linha não desligada. Se UMA das linhas é a matrícula que já tem esse
    /// CPF no banco, ela é mantida (o cadastro dela não muda por causa de uma linha nova errada) e só as
    /// outras são rejeitadas, citando a dona do CPF. Se nenhuma é dona (ou mais de uma), todas são rejeitadas.
    /// </summary>
    private static List<HrRow> RejectDuplicateCpfs(List<HrRow> rows, Dictionary<string, EmployeeSnapshot> byNumber, List<HrImportIssue> issues)
    {
        var rejected = new HashSet<HrRow>();
        foreach (var group in rows.GroupBy(r => r.Cpf).Where(g => g.Count(r => r.Status != EmployeeStatus.Terminated) > 1))
        {
            var candidates = group.Where(r => r.Status != EmployeeStatus.Terminated).ToList();
            var owners = candidates
                .Where(r => byNumber.TryGetValue(r.EmployeeNumber, out var current) && current.Cpf == r.Cpf)
                .ToList();

            if (owners.Count == 1)
            {
                var owner = owners[0];
                foreach (var row in candidates.Where(r => r != owner))
                {
                    rejected.Add(row);
                    issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Error,
                        $"CPF igual ao da matrícula {owner.EmployeeNumber} (linha {owner.RowNumber}), que já tem esse CPF."));
                }
                continue;
            }

            var lines = string.Join(", ", candidates.Select(r => r.RowNumber));
            foreach (var row in candidates)
            {
                rejected.Add(row);
                issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Error,
                    $"CPF repetido no arquivo em mais de uma matrícula não desligada (linhas {lines})."));
            }
        }
        return rejected.Count == 0 ? rows : rows.Where(r => !rejected.Contains(r)).ToList();
    }

    private static List<HrRow> RejectDuplicates(
        List<HrRow> rows, Func<HrRow, string> key, string message, List<HrImportIssue> issues, Func<IReadOnlyCollection<HrRow>, bool> isError)
    {
        var duplicated = rows.GroupBy(key).Where(g => g.Count() > 1 && isError(g.ToList())).ToList();
        if (duplicated.Count == 0) return rows;

        var rejected = new HashSet<HrRow>();
        foreach (var group in duplicated)
        {
            var lines = string.Join(", ", group.Select(r => r.RowNumber));
            foreach (var row in group)
            {
                rejected.Add(row);
                issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Error, string.Format(CultureInfo.InvariantCulture, message, lines)));
            }
        }
        return rows.Where(r => !rejected.Contains(r)).ToList();
    }

    /// <summary>
    /// Depois da importação o CPF não pode estar em duas matrículas não desligadas. Se o CPF pertence a
    /// outra matrícula que estará desligada (já está, ou será desligada agora), é só um aviso de possível readmissão.
    /// </summary>
    private static List<HrRow> ResolveCpfConflicts(
        List<HrRow> rows, IReadOnlyList<EmployeeSnapshot> existing, Dictionary<string, EmployeeSnapshot> byNumber,
        HashSet<string> presentNumbers, List<HrImportIssue> issues)
    {
        var rowsByNumber = rows.ToDictionary(r => r.EmployeeNumber, StringComparer.Ordinal);
        EmployeeStatus FinalStatus(EmployeeSnapshot e) =>
            rowsByNumber.TryGetValue(e.EmployeeNumber, out var r) ? r.Status
            : presentNumbers.Contains(e.EmployeeNumber) ? e.Status // linha rejeitada: fica como está
            : EmployeeStatus.Terminated;                            // ausente: será desligado

        var byCpf = existing.GroupBy(e => e.Cpf).ToDictionary(g => g.Key, g => g.ToList());
        var accepted = new List<HrRow>();
        foreach (var row in rows)
        {
            var others = byCpf.TryGetValue(row.Cpf, out var list)
                ? list.Where(e => e.EmployeeNumber != row.EmployeeNumber).ToList()
                : new List<EmployeeSnapshot>();

            var stillActive = others.FirstOrDefault(e => FinalStatus(e) != EmployeeStatus.Terminated);
            if (stillActive is not null && row.Status != EmployeeStatus.Terminated)
            {
                issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Error,
                    $"CPF já pertence à matrícula {stillActive.EmployeeNumber}, que continua ativa."));
                continue;
            }

            var previous = others.FirstOrDefault(e => FinalStatus(e) == EmployeeStatus.Terminated);
            if (previous is not null && !byNumber.ContainsKey(row.EmployeeNumber) && row.Status != EmployeeStatus.Terminated)
                issues.Add(new HrImportIssue(row.RowNumber, row.EmployeeNumber, HrIssueSeverity.Warning,
                    $"Possível readmissão: o CPF já pertence à matrícula desligada {previous.EmployeeNumber}."));

            accepted.Add(row);
        }
        return accepted;
    }

    private static List<FieldChange> Diff(EmployeeSnapshot current, HrRow row)
    {
        var changes = new List<FieldChange>();
        void Add(string field, string? before, string? after)
        {
            if (!string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.Ordinal))
                changes.Add(new FieldChange(field, before, after));
        }

        Add("Nome", current.FullName, row.FullName);
        // CPF compara o valor real, mas só expõe a versão mascarada.
        if (current.Cpf != row.Cpf)
            changes.Add(new FieldChange("CPF", MaskCpf(current.Cpf), MaskCpf(row.Cpf)));
        Add("Data admissão", FormatDate(current.AdmissionDate), FormatDate(row.AdmissionDate));
        Add("Situação", StatusLabel(current.Status), StatusLabel(row.Status));
        Add("Data demissão", FormatDate(current.TerminationDate), FormatDate(row.Status == EmployeeStatus.Terminated ? row.TerminationDate : null));
        Add("Cargo", current.JobTitleCode, row.JobTitleCode);
        Add("Setor (Uni.Org)", current.DepartmentCode, row.DepartmentCode);
        Add("Gerência", current.ManagementUnitCode, row.ManagementUnitCode);
        Add("Grupo", GroupLabel(current.Group), GroupLabel(row.Group));
        Add("Turma", current.Team, row.Team);
        return changes;
    }

    private static List<FieldChange> Describe(HrRow row) =>
    [
        new("Situação", null, StatusLabel(row.Status)),
        new("Cargo", null, $"{row.JobTitleCode} - {row.JobTitleName}"),
        new("Setor (Uni.Org)", null, $"{row.DepartmentCode} - {row.DepartmentName}"),
        new("Gerência", null, row.ManagementUnitCode),
    ];

    public static string MaskCpf(string? cpf) =>
        cpf is { Length: 11 } ? $"***.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-**" : "***";

    public static string StatusLabel(EmployeeStatus status) => status switch
    {
        EmployeeStatus.Active => "Ativo",
        EmployeeStatus.OnLeave => "Afastado",
        EmployeeStatus.Terminated => "Desligado",
        EmployeeStatus.Inactive => "Inativo",
        EmployeeStatus.PendingAdmission => "Admissão pendente",
        _ => status.ToString()
    };

    public static string? GroupLabel(EmployeeGroup? group) => group switch
    {
        EmployeeGroup.Field => "Campo",
        EmployeeGroup.Industry => "Indústria",
        _ => null
    };

    private static string? FormatDate(DateOnly? date) => date?.ToString(HrRowValidator.DateFormat, CultureInfo.InvariantCulture);

    private static string Fingerprint(IEnumerable<PlannedChange> changes, IEnumerable<string> unchanged, IEnumerable<HrImportIssue> issues)
    {
        var sb = new StringBuilder();
        foreach (var c in changes)
        {
            sb.Append((int)c.Kind).Append('|').Append(c.EmployeeNumber);
            foreach (var f in c.Changes) sb.Append('|').Append(f.Field).Append('=').Append(f.OldValue).Append('>').Append(f.NewValue);
            sb.Append('\n');
        }
        foreach (var u in unchanged) sb.Append("=|").Append(u).Append('\n');
        foreach (var i in issues) sb.Append("!|").Append(i.RowNumber).Append('|').Append(i.Message).Append('\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
    }
}
