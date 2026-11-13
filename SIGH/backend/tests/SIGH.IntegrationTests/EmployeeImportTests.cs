using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;
using Xunit.Abstractions;
using R = SIGH.IntegrationTests.Infrastructure.HrXlsxBuilder.Row;

namespace SIGH.IntegrationTests;

/// <summary>Importação da exportação do SAP, contra SQL Server real, com planilhas sintéticas geradas em memória.</summary>
public class EmployeeImportTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public EmployeeImportTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _output = output;
    }

    // Carga inicial: 5 funcionários. A 50090005 tem CPF que começa com zero, enviado como NÚMERO
    // (o Excel tira o zero); a 50090004 tem matrícula numérica sem os zeros à esquerda.
    private static readonly string CpfWithLeadingZero = HrXlsxBuilder.Cpf("012345678");

    private static byte[] InitialLoad() => new HrXlsxBuilder()
        .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
        .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Group: "Campo", ManagementUnit: "USGA141", DeptCode: "60002000", DeptName: "Corte"))
        .Add(new R("50090003", "Carla Sintética Três", HrXlsxBuilder.Cpf(3), Status: "Afastado"))
        .Add(new R(50090004L, "Davi Sintético Quatro", HrXlsxBuilder.Cpf(4)))
        .Add(new R("50090005", "Eva Sintética Cinco", long.Parse(CpfWithLeadingZero)))
        .Build();

    [Fact]
    public async Task Preview_WritesNothing_AndConfirm_AppliesTheInitialLoad()
    {
        using var admin = await AdminAsync();
        var file = InitialLoad();

        var preview = await PreviewAsync(admin, file);
        preview.Summary.Should().BeEquivalentTo(new ImportSummaryDto(5, 5, 0, 0, 0, 0, 0, 0, preview.Summary.Warnings));
        preview.Created.Select(c => c.EmployeeNumber).Should().BeEquivalentTo("50090001", "50090002", "50090003", "50090004", "50090005");
        preview.FileSha256.Should().HaveLength(64);
        (await CountEmployeesAsync()).Should().Be(0, "a pré-visualização não grava");

        var confirm = await ConfirmAsync(admin, preview.BatchId, file);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK, await confirm.Content.ReadAsStringAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var employees = await db.Employees.AsNoTracking().ToListAsync();
        employees.Should().HaveCount(5).And.OnlyContain(e => e.CompanyId == DatabaseSeeder.DefaultCompanyId);

        var eva = employees.Single(e => e.EmployeeNumber == "50090005");
        eva.Cpf.Should().Be(CpfWithLeadingZero).And.StartWith("0", "o zero à esquerda do CPF é preservado");
        employees.Should().Contain(e => e.EmployeeNumber == "50090004", "matrícula numérica completada com zeros (8 dígitos)");
        employees.Single(e => e.EmployeeNumber == "50090003").Status.Should().Be(EmployeeStatus.OnLeave);
        var bruno = employees.Single(e => e.EmployeeNumber == "50090002");
        bruno.Group.Should().Be(EmployeeGroup.Field);
        bruno.Team.Should().Be("A");
        (await db.ManagementUnits.SingleAsync(m => m.Id == bruno.ManagementUnitId)).Name.Should().Be("Agrícola Rurícola");
        (await db.JobTitles.SingleAsync(j => j.Id == bruno.JobTitleId)).Code.Should().Be("50001234");
        (await db.Departments.SingleAsync(d => d.Id == bruno.DepartmentId)).Name.Should().Be("Corte");

        var batch = await db.EmployeeImportBatches.AsNoTracking().SingleAsync();
        batch.Status.Should().Be(SIGH.Domain.Employees.Entities.EmployeeImportBatchStatus.Applied);
        batch.CreatedCount.Should().Be(5);
        batch.AppliedByUserId.Should().Be(CustomWebApplicationFactory.AdminUserId);
    }

    [Fact]
    public async Task UpdateFile_ProducesUpdatesTerminationsAbsencesReactivationsAndReadmissionWarnings()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, InitialLoad());

        // Primeiro desliga a 50090004 pelo status, para depois reativá-la.
        await ImportAsync(admin, new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
            .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Group: "Campo", ManagementUnit: "USGA141", DeptCode: "60002000", DeptName: "Corte"))
            .Add(new R("50090003", "Carla Sintética Três", HrXlsxBuilder.Cpf(3), Status: "Afastado"))
            .Add(new R("50090004", "Davi Sintético Quatro", HrXlsxBuilder.Cpf(4), Status: "Desligado", Termination: "10/10/2026"))
            .Add(new R("50090005", "Eva Sintética Cinco", CpfWithLeadingZero))
            .Build());

        var update = new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1), JobCode: "50009999", JobName: "Supervisora"))   // alterada
            .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Status: "Demitido", Termination: "31/10/2026",
                Group: "Campo", ManagementUnit: "USGA141", DeptCode: "60002000", DeptName: "Corte"))                            // desligado pelo status
            // 50090003 ausente → desligada por ausência
            .Add(new R("50090004", "Davi Sintético Quatro", HrXlsxBuilder.Cpf(4), Admission: "01/11/2026"))                   // reativado
            .Add(new R("50090005", "Eva Sintética Cinco", CpfWithLeadingZero))                                                // sem mudança
            .Add(new R("50090006", "Fábio Sintético Seis", HrXlsxBuilder.Cpf(6), ManagementUnit: "USGA999"))                  // novo, gerência desconhecida
            .Add(new R("50090007", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Admission: "05/11/2026"))                    // readmissão do CPF do Bruno
            .Build();

        var preview = await PreviewAsync(admin, update);

        preview.Created.Select(c => c.EmployeeNumber).Should().BeEquivalentTo("50090006", "50090007");
        preview.Updated.Select(c => c.EmployeeNumber).Should().BeEquivalentTo("50090001", "50090002");
        preview.Updated.Single(c => c.EmployeeNumber == "50090001").Changes.Should().ContainSingle(c => c.Field == "Cargo" && c.OldValue == "50001234" && c.NewValue == "50009999");
        preview.Updated.Single(c => c.EmployeeNumber == "50090002").Changes.Should().Contain(c => c.Field == "Situação" && c.NewValue == "Desligado");
        preview.ToTerminate.Should().ContainSingle(c => c.EmployeeNumber == "50090003");
        preview.Reactivated.Should().ContainSingle(c => c.EmployeeNumber == "50090004");
        preview.Unchanged.Should().Equal("50090005");
        preview.Errors.Should().BeEmpty();
        preview.Warnings.Should().Contain(w => w.EmployeeNumber == "50090007" && w.Message.Contains("Possível readmissão") && w.Message.Contains("50090002"));
        preview.Errors.Should().NotContain(e => e.Message.Contains("CPF repetido"));
        preview.Warnings.Should().Contain(w => w.Message.Contains("USGA999"));
        preview.Warnings.Should().NotContain(w => w.Message.Contains(HrXlsxBuilder.Cpf(2)), "mensagens nunca trazem o CPF");

        // 1 ausente em 4 ativos (25%) > 10%: exige confirmação explícita.
        preview.RequiresMassTerminationConfirmation.Should().BeTrue();
        (await ConfirmAsync(admin, preview.BatchId, update)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ConfirmAsync(admin, preview.BatchId, update, confirmMassTermination: true)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var all = await db.Employees.AsNoTracking().ToDictionaryAsync(e => e.EmployeeNumber);
        all.Should().HaveCount(7, "ninguém é apagado");
        all["50090002"].Status.Should().Be(EmployeeStatus.Terminated);
        all["50090002"].TerminationDate.Should().Be(new DateOnly(2026, 10, 31));
        all["50090003"].Status.Should().Be(EmployeeStatus.Terminated);
        all["50090003"].TerminationDate.Should().Be(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Brasilia()).DateTime));
        all["50090003"].TerminationReason.Should().StartWith(EmployeeImportPlanner.AbsenceReasonPrefix);
        all["50090004"].Status.Should().Be(EmployeeStatus.Active);
        all["50090004"].TerminationDate.Should().BeNull();
        all["50090007"].Cpf.Should().Be(all["50090002"].Cpf);
        (await db.ManagementUnits.SingleAsync(m => m.Code == "USGA999")).Name.Should().Be("USGA999");

        // A auditoria registra os campos alterados (CPF sempre mascarado como [redacted]).
        var audit = await db.AuditLogs.Where(a => a.Entity == "Employee" && a.EntityId == all["50090001"].Id.ToString() && a.Action == "Updated").ToListAsync();
        audit.Should().ContainSingle().Which.NewValues.Should().Contain("JobTitleId");
    }

    [Fact]
    public async Task ImportingTheSameFileTwice_ChangesNothing()
    {
        using var admin = await AdminAsync();
        var file = InitialLoad();
        await ImportAsync(admin, file);
        var auditBefore = await CountAsync("SELECT COUNT(*) FROM AuditLogs WHERE Entity = 'Employee'");

        var second = await PreviewAsync(admin, file);

        second.Summary.Should().BeEquivalentTo(new ImportSummaryDto(5, 0, 0, 0, 0, 0, 5, 0, second.Summary.Warnings));
        (await ConfirmAsync(admin, second.BatchId, file)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CountAsync("SELECT COUNT(*) FROM AuditLogs WHERE Entity = 'Employee'")).Should().Be(auditBefore);
    }

    [Fact]
    public async Task SensitiveColumns_NeverReachTheDatabaseOrTheLogs()
    {
        var logs = new ConcurrentQueue<string>();
        // A API usa Serilog com ReadFrom.Services: um ILogEventSink no DI recebe todos os eventos.
        await using var host = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddSingleton<Serilog.Core.ILogEventSink>(new CapturingSink(logs))));
        using var admin = await AdminAsync(host);
        var file = InitialLoad();

        var preview = await PreviewAsync(admin, file);
        (await ConfirmAsync(admin, preview.BatchId, file)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Varre TODAS as colunas de texto de TODAS as tabelas.
        await using var connection = new SqlConnection(await SqlServerTestDatabase.GetConnectionStringAsync());
        await connection.OpenAsync();
        var columns = new List<(string Table, string Column)>();
        await using (var cmd = new SqlCommand(
            "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE DATA_TYPE IN ('nvarchar','varchar','nchar','char','ntext','text') AND TABLE_SCHEMA = 'dbo'", connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) columns.Add((reader.GetString(0), reader.GetString(1)));

        // GUIDs são removidos antes da busca: "00000-0" aparece dentro de qualquer id (10000000-0000-...).
        var guid = new System.Text.RegularExpressions.Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        foreach (var (table, column) in columns)
        {
            await using var read = new SqlCommand($"SELECT CAST([{column}] AS nvarchar(max)) FROM [{table}] WHERE [{column}] IS NOT NULL", connection);
            await using var values = await read.ExecuteReaderAsync();
            while (await values.ReadAsync())
            {
                var text = guid.Replace(values.GetString(0), string.Empty);
                foreach (var marker in HrXlsxBuilder.SensitiveMarkers)
                    text.Should().NotContain(marker, $"\"{marker}\" não pode estar em {table}.{column}");
            }
        }

        var allLogs = guid.Replace(string.Join("\n", logs), string.Empty);
        allLogs.Should().Contain("pré-visualização", "a captura de logs funcionou");
        foreach (var marker in HrXlsxBuilder.SensitiveMarkers)
            allLogs.Should().NotContain(marker);
        allLogs.Should().NotContain(HrXlsxBuilder.Cpf(1)).And.NotContain("Ana Sintética");
    }

    [Fact]
    public async Task UnknownStatus_RejectsOnlyThatRow_WithAClearMessage()
    {
        using var admin = await AdminAsync();
        var file = new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
            .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Status: "Aposentado por invalidez"))
            .Build();

        var preview = await PreviewAsync(admin, file);

        preview.Created.Should().ContainSingle(c => c.EmployeeNumber == "50090001");
        var error = preview.Errors.Should().ContainSingle().Subject;
        error.EmployeeNumber.Should().Be("50090002");
        error.Row.Should().Be(3);
        error.Message.Should().Contain("Aposentado por invalidez").And.Contain("desconhecida");
        error.Message.Should().NotContain(HrXlsxBuilder.Cpf(2));
    }

    [Fact]
    public async Task TerminatedStatus_CreatesTheEmployeeAsTerminated()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
            .Add(new R("50090008", "Hugo Sintético Oito", HrXlsxBuilder.Cpf(8), Status: "Desligado", Termination: "15/03/2025"))
            .Build());

        await using var scope = Factory.Services.CreateAsyncScope();
        var hugo = await scope.ServiceProvider.GetRequiredService<SighDbContext>().Employees.AsNoTracking().SingleAsync(e => e.EmployeeNumber == "50090008");
        hugo.Status.Should().Be(EmployeeStatus.Terminated);
        hugo.TerminationDate.Should().Be(new DateOnly(2025, 3, 15));
    }

    [Fact]
    public async Task TerminatedStatusWithoutTerminationDate_IsRejected()
    {
        using var admin = await AdminAsync();
        var preview = await PreviewAsync(admin, new HrXlsxBuilder()
            .Add(new R("50090008", "Hugo Sintético Oito", HrXlsxBuilder.Cpf(8), Status: "Demitido"))
            .Build());

        preview.Errors.Should().ContainSingle(e => e.Message.Contains("Data demissão"));
    }

    [Fact]
    public async Task InvalidRows_AreRejected_WithoutHidingTheRest()
    {
        using var admin = await AdminAsync();
        var preview = await PreviewAsync(admin, new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
            .Add(new R("50090002", "Bruno Sintético Dois", "12345678900"))                      // CPF com dígito errado
            .Add(new R("5009003", "Carla Sintética Três", HrXlsxBuilder.Cpf(3)))                // matrícula com 7 dígitos (texto)
            .Add(new R("50090004", "", HrXlsxBuilder.Cpf(4)))                                   // sem nome
            .Add(new R("50090005", "Eva Sintética Cinco", HrXlsxBuilder.Cpf(5), Admission: "2020-02-01")) // data fora do formato
            .Add(new R("50090006", "Fábio Sintético Seis", HrXlsxBuilder.Cpf(6)))
            .Add(new R("50090007", "Gil Sintético Sete", HrXlsxBuilder.Cpf(6)))                 // CPF repetido em duas matrículas ativas
            .Build());

        preview.Created.Select(c => c.EmployeeNumber).Should().Equal("50090001");
        preview.Errors.Select(e => e.EmployeeNumber).Should().BeEquivalentTo("50090002", "5009003", "50090004", "50090005", "50090006", "50090007");
        preview.Errors.Should().Contain(e => e.EmployeeNumber == "50090002" && e.Message.Contains("dígitos verificadores"));
        preview.Errors.Should().Contain(e => e.EmployeeNumber == "50090005" && e.Message.Contains("dd/MM/yyyy"));
        preview.Errors.Where(e => e.EmployeeNumber is "50090006" or "50090007").Should().OnlyContain(e => e.Message.Contains("CPF repetido"));
        preview.Errors.Should().OnlyContain(e => e.Row > 1);
        preview.Errors.Should().NotContain(e => e.Message.Contains("12345678900"));
    }

    [Fact]
    public async Task RejectedRows_DoNotCountAsAbsent()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, InitialLoad());

        var preview = await PreviewAsync(admin, new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1)))
            .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Status: "???"))
            .Add(new R("50090003", "Carla Sintética Três", HrXlsxBuilder.Cpf(3), Status: "Afastado"))
            .Add(new R(50090004L, "Davi Sintético Quatro", HrXlsxBuilder.Cpf(4)))
            .Add(new R("50090005", "Eva Sintética Cinco", long.Parse(CpfWithLeadingZero)))
            .Build());

        preview.ToTerminate.Should().BeEmpty("a 50090002 está no arquivo, só que com erro");
        preview.Errors.Should().ContainSingle(e => e.EmployeeNumber == "50090002");
    }

    [Fact]
    public async Task MissingRequiredColumn_IsReportedByName()
    {
        using var admin = await AdminAsync();

        var response = await PostFileAsync(admin, "/api/v1/employees/import/preview", new HrXlsxBuilder().Without("Chave Organização")
            .Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1))).Build());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<Result<EmployeeImportPreviewDto>>())!.Message.Should().Contain("\"Chave Organização\"");
    }

    [Fact]
    public async Task Confirm_RequiresTheSameFile_AndAValidBatch()
    {
        using var admin = await AdminAsync();
        var file = InitialLoad();
        var preview = await PreviewAsync(admin, file);

        var other = new HrXlsxBuilder().Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1))).Build();
        (await ConfirmAsync(admin, preview.BatchId, other)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ConfirmAsync(admin, Guid.NewGuid(), file)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31));
        var expired = await ConfirmAsync(admin, preview.BatchId, file);
        expired.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await expired.Content.ReadFromJsonAsync<Result<EmployeeImportResultDto>>())!.Message.Should().Contain("expirou");
        (await CountEmployeesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Confirm_IsRejectedIfTheDatabaseChangedSinceThePreview()
    {
        using var admin = await AdminAsync();
        var file = InitialLoad();
        var preview = await PreviewAsync(admin, file);

        // Outra importação aplicada no meio do caminho.
        await ImportAsync(admin, new HrXlsxBuilder().Add(new R("50090001", "Ana Sintética Um", HrXlsxBuilder.Cpf(1))).Build());

        (await ConfirmAsync(admin, preview.BatchId, file)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Import_RequiresEmployeesImport()
    {
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "unprivileged@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        (await PostFileAsync(client, "/api/v1/employees/import/preview", InitialLoad())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/employees/import/batches")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task History_ListsTheBatchesWithCountsAndNoSensitiveData()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, InitialLoad());

        var page = (await admin.GetFromJsonAsync<Result<PagedResult<EmployeeImportBatchDto>>>("/api/v1/employees/import/batches"))!.Data!;

        var batch = page.Items.Should().ContainSingle().Subject;
        batch.Status.Should().Be("Applied");
        batch.Summary.Created.Should().Be(5);
        batch.AppliedByName.Should().Be("Administrador do Sistema");
    }

    [Fact]
    public async Task SixThousandRowsWith72Columns_PreviewAndConfirmEachUnder15Seconds()
    {
        using var admin = await AdminAsync();
        // 21 colunas de base + 51 extras = 72 colunas.
        var builder = new HrXlsxBuilder(extraColumns: 51);
        for (var i = 0; i < 6000; i++)
        {
            var mu = new[] { "USGA131", "USGA132", "USGA133", "USGA134", "USGA135", "USGA136", "USGA137", "USGA138", "USGA141" }[i % 9];
            builder.Add(new R((51000000 + i).ToString(), $"Pessoa Sintética {i:0000}", HrXlsxBuilder.Cpf(1000 + i),
                Status: i % 50 == 0 ? "Afastado" : "Ativo", JobCode: $"5000{i % 40:0000}", JobName: $"Cargo {i % 40}",
                DeptCode: $"6000{i % 120:0000}", DeptName: $"Setor {i % 120}", ManagementUnit: mu, Group: i % 2 == 0 ? "Campo" : "Indústria",
                Team: $"T{i % 6}"));
        }
        var file = builder.Build();
        _output.WriteLine($"Arquivo sintético: {file.Length / 1024} KB, 6000 linhas x 72 colunas");

        var watch = Stopwatch.StartNew();
        var preview = await PreviewAsync(admin, file);
        var previewTime = watch.Elapsed;
        preview.Summary.Created.Should().Be(6000);
        preview.Errors.Should().BeEmpty();

        watch.Restart();
        var confirm = await ConfirmAsync(admin, preview.BatchId, file);
        var confirmTime = watch.Elapsed;
        confirm.StatusCode.Should().Be(HttpStatusCode.OK, await confirm.Content.ReadAsStringAsync());

        // Segunda importação (tudo sem mudança): também precisa ser rápida.
        watch.Restart();
        var again = await PreviewAsync(admin, file);
        var secondPreviewTime = watch.Elapsed;
        again.Summary.Unchanged.Should().Be(6000);

        _output.WriteLine($"Pré-visualização (6000 novos): {previewTime.TotalSeconds:0.00} s");
        _output.WriteLine($"Confirmação (6000 criações): {confirmTime.TotalSeconds:0.00} s");
        _output.WriteLine($"Pré-visualização (6000 sem mudança): {secondPreviewTime.TotalSeconds:0.00} s");
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "sigh-import-timings.txt"),
            $"preview={previewTime.TotalSeconds:0.00};confirm={confirmTime.TotalSeconds:0.00};preview2={secondPreviewTime.TotalSeconds:0.00}");

        previewTime.Should().BeLessThan(TimeSpan.FromSeconds(15));
        confirmTime.Should().BeLessThan(TimeSpan.FromSeconds(15));
        secondPreviewTime.Should().BeLessThan(TimeSpan.FromSeconds(15));
    }

    // ---- helpers ----

    private Task<HttpClient> AdminAsync() => AdminAsync(Factory);

    private static async Task<HttpClient> AdminAsync(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.Timeout = TimeSpan.FromMinutes(2);
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<HttpResponseMessage> PostFileAsync(HttpClient client, string url, byte[] file, bool? confirmMassTermination = null)
    {
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(content, "file", "exportacao_sap.xlsx");
        if (confirmMassTermination is { } flag) form.Add(new StringContent(flag ? "true" : "false"), "confirmMassTermination");
        return await client.PostAsync(url, form);
    }

    private static async Task<EmployeeImportPreviewDto> PreviewAsync(HttpClient client, byte[] file)
    {
        var response = await PostFileAsync(client, "/api/v1/employees/import/preview", file);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<EmployeeImportPreviewDto>>())!.Data!;
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid batchId, byte[] file, bool confirmMassTermination = false) =>
        PostFileAsync(client, $"/api/v1/employees/import/{batchId}/confirm", file, confirmMassTermination);

    private static async Task ImportAsync(HttpClient client, byte[] file)
    {
        var preview = await PreviewAsync(client, file);
        var response = await ConfirmAsync(client, preview.BatchId, file, confirmMassTermination: true);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<int> CountEmployeesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SighDbContext>().Employees.CountAsync();
    }

    private static async Task<int> CountAsync(string sql)
    {
        await using var connection = new SqlConnection(await SqlServerTestDatabase.GetConnectionStringAsync());
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static TimeZoneInfo Brasilia() =>
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var tz) ? tz : TimeZoneInfo.Utc;

    /// <summary>Guarda a mensagem renderizada, todas as propriedades e a exceção de cada evento.</summary>
    private sealed class CapturingSink(ConcurrentQueue<string> sink) : Serilog.Core.ILogEventSink
    {
        public void Emit(Serilog.Events.LogEvent logEvent) =>
            sink.Enqueue(logEvent.RenderMessage() + " " +
                         string.Join(" ", logEvent.Properties.Select(p => $"{p.Key}={p.Value}")) + " " + logEvent.Exception);
    }
}
