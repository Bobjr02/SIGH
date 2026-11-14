using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.Common;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.Evidences;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Employees.Import;
using SIGH.Application.Interfaces;
using SIGH.Application.Users.CreateUser;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Repositories;
using SIGH.Persistence.Context;

namespace SIGH.Api.Demo;

/// <summary>
/// Dados de demonstração FICTÍCIOS, criados pelos SERVIÇOS REAIS (importador de funcionários, criação de
/// usuário, troca de senha, registro/aprovação/rejeição/cancelamento de medidas e upload de evidência),
/// nunca por inserção direta no banco. Cada etapa roda "como" o usuário certo (o admin importa e cria
/// usuários; o auxiliar registra; o encarregado aprova), com as permissões efetivas dele.
/// Idempotente: cada parte confere se já existe antes de criar.
/// </summary>
public sealed class DemoDataSeeder
{
    public const string Domain = "demo.local";

    public sealed record DemoUser(string Login, string FullName, string Cpf, string[] Roles, string[] Units);

    /// <summary>Usuários de demonstração (e-mail = login@demo.local). CPFs sintéticos válidos.</summary>
    public static readonly IReadOnlyList<DemoUser> Users =
    [
        new("auxiliar", "AUXILIAR DE ESCRITORIO DEMO TESTE", SyntheticCpf("900000001"), [SystemRoles.HrRegistrar], []),
        new("encarregado", "ENCARREGADO DO RH DEMO TESTE", SyntheticCpf("900000002"), [SystemRoles.HrApprover], []),
        new("juridico", "ADVOGADA DO JURIDICO DEMO TESTE", SyntheticCpf("900000003"), [SystemRoles.Legal], []),
        new("gestor.industrial", "GESTOR DA INDUSTRIAL DEMO TESTE", SyntheticCpf("900000004"), [SystemRoles.Manager], ["USGA131"]),
        new("gestor.agricola", "GESTOR AGRICOLA DEMO TESTE", SyntheticCpf("900000005"), [SystemRoles.Manager], ["USGA137", "USGA141"]),
        new("preposto", "PREPOSTO DO RH DEMO TESTE", SyntheticCpf("900000006"), [SystemRoles.HrRegistrar, SystemRoles.HrApprover], []),
    ];

    private enum Outcome { Approved, Pending, Rejected, Cancelled }

    private sealed record DemoRecord(int Number, string EmployeeNumber, DisciplinaryMeasureType Type, string Infraction, int DaysAgo, Outcome Outcome,
        int SuspensionDays = 0, bool WithAttachment = false);

    /// <summary>3 aprovadas, 1 suspensão aprovada, 2 pendentes, 1 rejeitada aguardando correção e 1 cancelada.</summary>
    private static readonly DemoRecord[] Records =
    [
        new(1, "50090006", DisciplinaryMeasureType.WrittenWarning, "EPI", 20, Outcome.Approved, WithAttachment: true), // USGA131
        new(2, "50090000", DisciplinaryMeasureType.VerbalWarning, "ATRASO", 18, Outcome.Approved),                     // USGA132
        new(3, "50090001", DisciplinaryMeasureType.WrittenWarning, "FALTA_INJUSTIFICADA", 15, Outcome.Approved),       // USGA141
        new(4, "50090002", DisciplinaryMeasureType.Suspension, "FALTA_INJUSTIFICADA", 12, Outcome.Approved, SuspensionDays: 2), // USGA141
        new(5, "50090012", DisciplinaryMeasureType.WrittenWarning, "ATRASO", 6, Outcome.Pending),                      // USGA131
        new(6, "50090003", DisciplinaryMeasureType.VerbalWarning, "FALTA_INJUSTIFICADA", 4, Outcome.Pending),          // USGA136
        new(7, "50090009", DisciplinaryMeasureType.WrittenWarning, "EPI", 9, Outcome.Rejected),                        // USGA136
        new(8, "50090015", DisciplinaryMeasureType.VerbalWarning, "ATRASO", 10, Outcome.Cancelled),                    // USGA134
    ];

    private static readonly (string Code, string Name, InfractionSeverity Severity)[] InfractionTypes =
    [
        ("FALTA_INJUSTIFICADA", "Faltas injustificadas", InfractionSeverity.Moderate),
        ("EPI", "Falta de uso de EPI", InfractionSeverity.High),
        ("ATRASO", "Atrasos reiterados", InfractionSeverity.Low),
    ];

    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly DemoDataOptions _options;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(IServiceScopeFactory scopes, IConfiguration configuration, IOptions<DemoDataOptions> options, IDateTimeProvider clock, ILogger<DemoDataSeeder> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public static string EmailOf(string login) => $"{login}@{Domain}";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var adminEmail = _configuration["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
            var adminId = await UserIdAsync(adminEmail, cancellationToken)
                          ?? throw new InvalidOperationException("Dados de demonstração exigem o administrador do seed (Seed:AdminEmail e Seed:AdminPassword).");

            await ImportEmployeesAsync(adminId, cancellationToken);
            _logger.LogInformation("Demo: importação de histórico disciplinar não existe nesta versão; etapa pulada.");
            var infractions = await EnsureInfractionTypesAsync(adminId, cancellationToken);
            var users = await EnsureUsersAsync(adminId, cancellationToken);
            await EnsureRecordsAsync(users, infractions, cancellationToken);
            _logger.LogInformation("Demo: dados de demonstração prontos ({Users} usuários, {Records} medidas).", users.Count, Records.Length);
        }
        catch (Exception ex)
        {
            // A API continua de pé: a falha fica no log e os scripts locais mostram o aviso.
            _logger.LogError(ex, "Demo: falha ao criar os dados de demonstração.");
        }
    }

    // ---- a. funcionários (importador do SAP) ----

    private async Task ImportEmployeesAsync(Guid adminId, CancellationToken ct)
    {
        await RunAsAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<SighDbContext>();
            if (await db.Employees.AnyAsync(ct))
            {
                _logger.LogInformation("Demo: funcionários já importados.");
                return;
            }

            var path = Path.Combine(DataPath(), "importacao-funcionarios", "funcionarios_carga_inicial.xlsx");
            var bytes = await File.ReadAllBytesAsync(path, ct);
            var import = sp.GetRequiredService<IEmployeeImportService>();
            var preview = await import.PreviewAsync(new MemoryStream(bytes), Path.GetFileName(path), adminId, ct);
            if (!preview.Success) throw new InvalidOperationException($"Pré-visualização da carga de funcionários falhou: {preview.Message}");
            var confirm = await import.ConfirmAsync(preview.Data!.BatchId, new MemoryStream(bytes), false, adminId, ct);
            if (!confirm.Success) throw new InvalidOperationException($"Confirmação da carga de funcionários falhou: {confirm.Message}");
            _logger.LogInformation("Demo: {Count} funcionários fictícios importados.", confirm.Data!.Summary.Created);
        });
    }

    // ---- motivos ----

    private async Task<Dictionary<string, Guid>> EnsureInfractionTypesAsync(Guid adminId, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>();
        await RunAsAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<SighDbContext>();
            foreach (var (code, name, severity) in InfractionTypes)
            {
                var id = await db.InfractionTypes.Where(i => i.Code == code).Select(i => (Guid?)i.Id).FirstOrDefaultAsync(ct);
                if (id is null)
                {
                    var created = await sp.GetRequiredService<ICreateInfractionTypeUseCase>().ExecuteAsync(new CreateInfractionTypeRequest(code, name, severity), ct);
                    if (!created.Success) throw new InvalidOperationException($"Motivo {code}: {created.Message}");
                    id = created.Data!.Id;
                }
                result[code] = id.Value;
            }
        });
        return result;
    }

    // ---- c. usuários (criação real + troca de senha real: sem troca obrigatória depois) ----

    private async Task<Dictionary<string, Guid>> EnsureUsersAsync(Guid adminId, CancellationToken ct)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var user in Users)
        {
            var email = EmailOf(user.Login);
            if (await UserIdAsync(email, ct) is { } existing)
            {
                ids[user.Login] = existing;
                continue;
            }

            var initialPassword = RandomPassword();
            Guid id = Guid.Empty;
            await RunAsAsync(adminId, async sp =>
            {
                var db = sp.GetRequiredService<SighDbContext>();
                var roles = await db.Roles.Where(r => user.Roles.Contains(r.Name)).Select(r => r.Id).ToListAsync(ct);
                var units = await db.ManagementUnits.Where(m => user.Units.Contains(m.Code!)).Select(m => m.Id).ToListAsync(ct);
                var created = await sp.GetRequiredService<ICreateUserService>()
                    .CreateUserAsync(new CreateUserRequest(user.FullName, email, user.Cpf, initialPassword, roles, units), ct);
                id = created.Id;
            });

            // O próprio usuário "faz o primeiro acesso": troca a senha inicial pela senha de demonstração.
            await RunAsAsync(id, async sp =>
                await sp.GetRequiredService<IChangePasswordService>().ChangePasswordAsync(new ChangePasswordRequest(id, initialPassword, _options.DemoPassword!), ct));

            ids[user.Login] = id;
            _logger.LogInformation("Demo: usuário {Email} criado.", email);
        }
        return ids;
    }

    // ---- d. medidas em todos os estados ----

    private async Task EnsureRecordsAsync(Dictionary<string, Guid> users, Dictionary<string, Guid> infractions, CancellationToken ct)
    {
        var registrar = users["auxiliar"];
        var approver = users["encarregado"];
        var today = BusinessCalendar.Today(_clock.UtcNow);

        foreach (var demo in Records)
        {
            var tag = $"[DEMO-{demo.Number}]";
            Guid? existing = null;
            await RunAsAsync(registrar, async sp =>
                existing = await sp.GetRequiredService<SighDbContext>().DisciplinaryCases
                    .Where(c => c.Kind == DisciplinaryCaseKind.Record && c.Description.Contains(tag)).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct));
            if (existing is not null)
                continue;

            var occurrence = today.AddDays(-demo.DaysAgo);
            var measureDate = occurrence.AddDays(1);
            var input = new RecordInput(
                await EmployeeIdAsync(demo.EmployeeNumber, ct),
                infractions[demo.Infraction],
                demo.Type,
                [occurrence],
                measureDate,
                demo.Type == DisciplinaryMeasureType.Suspension ? measureDate.AddDays(1) : null,
                demo.Type == DisciplinaryMeasureType.Suspension ? measureDate.AddDays(demo.SuspensionDays) : null,
                $"{tag} Medida fictícia de demonstração: {DescriptionOf(demo)}",
                ConfirmPossibleDuplicate: true);

            Guid recordId = Guid.Empty;
            await RunAsAsync(registrar, async sp =>
                recordId = (await sp.GetRequiredService<IRecordService>().RegisterAsync(registrar, input, ct)).Record!.Summary.Id);

            if (demo.WithAttachment)
                await AttachLetterAsync(registrar, recordId, ct);

            switch (demo.Outcome)
            {
                case Outcome.Approved:
                    await RunAsAsync(approver, sp => sp.GetRequiredService<IRecordService>().ApproveAsync(approver, recordId, ct));
                    break;
                case Outcome.Rejected:
                    await RunAsAsync(approver, sp => sp.GetRequiredService<IRecordService>().RejectAsync(approver, recordId,
                        new ReasonInput("Data da ocorrência não confere com o cartão de ponto (demonstração)."), ct));
                    break;
                case Outcome.Cancelled:
                    await RunAsAsync(approver, sp => sp.GetRequiredService<IRecordService>().RejectAsync(approver, recordId,
                        new ReasonInput("Registrada para o funcionário errado (demonstração)."), ct));
                    await RunAsAsync(registrar, sp => sp.GetRequiredService<IRecordService>().CancelAsync(registrar, recordId,
                        new ReasonInput("Cancelada pelo registrante após a rejeição (demonstração)."), ct));
                    break;
            }
            _logger.LogInformation("Demo: medida {Tag} criada ({Outcome}).", tag, demo.Outcome);
        }
    }

    /// <summary>Carta assinada fictícia: um PDF gerado na hora, anexado pelos serviços de evidência.</summary>
    private Task AttachLetterAsync(Guid registrar, Guid recordId, CancellationToken ct) =>
        RunAsAsync(registrar, async sp =>
        {
            var evidence = await sp.GetRequiredService<IAddEvidenceUseCase>().ExecuteAsync(
                new AddEvidenceRequest(recordId, EvidenceType.Document, "Carta assinada (fictícia)", registrar, _clock.UtcNow,
                    Description: "Documento fictício gerado para a demonstração."), ct);
            if (!evidence.Success) throw new InvalidOperationException($"Anexo: {evidence.Message}");

            var pdf = FictitiousPdf();
            var upload = await sp.GetRequiredService<IUploadEvidenceFileUseCase>().ExecuteAsync(
                new UploadEvidenceFileRequest(recordId, evidence.Data!.Id, new MemoryStream(pdf), "carta-assinada-ficticia.pdf", pdf.Length, registrar), ct);
            if (!upload.Success) throw new InvalidOperationException($"Upload do anexo: {upload.Message}");
        });

    private static string DescriptionOf(DemoRecord demo) => demo.Infraction switch
    {
        "EPI" => "trabalhou sem os óculos de proteção na área da moenda.",
        "ATRASO" => "chegou atrasado ao turno em dias seguidos.",
        _ => "faltou ao turno sem justificativa."
    };

    // ---- execução "como" um usuário (serviços reais com as permissões efetivas dele) ----

    /// <summary>
    /// Cria um escopo com um HttpContext cujo usuário é <paramref name="userId"/>, com as claims de permissão
    /// calculadas como no login (perfis vigentes). CurrentUserService, escopo e auditoria funcionam como
    /// numa requisição desse usuário.
    /// </summary>
    private async Task RunAsAsync(Guid userId, Func<IServiceProvider, Task> action)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var user = await sp.GetRequiredService<IUserRepository>().GetByIdWithRolesAndPermissionsAsync(userId)
                   ?? throw new InvalidOperationException($"Usuário {userId} não encontrado.");

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Name, user.FullName) };
        claims.AddRange(user.GetActivePermissionCodes(_clock.UtcNow).Select(p => new Claim("permission", p)));

        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = sp,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "DemoSeed")),
        };
        try
        {
            await action(sp);
        }
        finally
        {
            accessor.HttpContext = previous;
        }
    }

    private async Task<Guid?> UserIdAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        await using var scope = _scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SighDbContext>().Users
            .Where(u => u.Email == email).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
    }

    private async Task<Guid> EmployeeIdAsync(string employeeNumber, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SighDbContext>().Employees
            .Where(e => e.EmployeeNumber == employeeNumber).Select(e => e.Id).SingleAsync(ct);
    }

    private string DataPath() =>
        !string.IsNullOrWhiteSpace(_options.DemoDataPath)
            ? _options.DemoDataPath
            : throw new InvalidOperationException("Seed:DemoDataPath não configurado (pasta SIGH/docs/referencias).");

    /// <summary>Senha inicial aleatória (descartada: o usuário troca logo em seguida).</summary>
    private static string RandomPassword() => $"Ini#{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}a";

    public static string SyntheticCpf(string base9)
    {
        var digits = base9.Select(c => c - '0').ToList();
        for (var round = 0; round < 2; round++)
        {
            var weight = digits.Count + 1;
            var rest = digits.Sum(d => d * weight--) % 11;
            digits.Add(rest < 2 ? 0 : 11 - rest);
        }
        return string.Concat(digits);
    }

    /// <summary>PDF mínimo e válido, com o texto "DOCUMENTO FICTICIO - DEMONSTRACAO".</summary>
    public static byte[] FictitiousPdf()
    {
        const string content = "BT /F1 18 Tf 40 120 Td (DOCUMENTO FICTICIO - DEMONSTRACAO) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 500 200] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) sb.Append($"{offset:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
