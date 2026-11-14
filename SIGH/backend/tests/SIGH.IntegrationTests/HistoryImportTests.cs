using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.History;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Employees.DecisionSupport;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Importação do histórico com os arquivos do pacote: funcionarios_carga_inicial.xlsx e depois
/// base_disciplinar_ficticia.xlsx, conferindo a tabela "Resultado esperado" do HISTORICO.md linha a linha.
/// </summary>
public class HistoryImportTests : IntegrationTestBase
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Referencias");
    private static byte[] History => File.ReadAllBytes(Path.Combine(FixtureDir, "base_disciplinar_ficticia.xlsx"));

    public HistoryImportTests(CustomWebApplicationFactory factory) : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ReferenceData.LoadEmployeesAsync(Factory);

        // Catálogo do RH (o HISTORICO.md pressupõe estes três motivos já cadastrados; só "USO DE CELULAR EM SERVIÇO" é novo).
        // Grafia diferente da planilha de propósito: a comparação é pelo nome normalizado.
        await ReferenceData.InfractionTypeAsync(Factory, "FALTA_INJUSTIFICADA", "Faltas injustificadas de forma recorrente");
        await ReferenceData.InfractionTypeAsync(Factory, "EPI", "Falta de utilização do EPI");
        await ReferenceData.InfractionTypeAsync(Factory, "NEGLIGENCIA", "Negligência ou imprudência no desempenho de suas funções");
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = Factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, byte[] file)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(file), "file", "base_disciplinar_ficticia.xlsx");
        return await client.PostAsync(url, form);
    }

    private static async Task<HistoryImportPreviewDto> PreviewAsync(HttpClient client, byte[] file)
    {
        var response = await PostAsync(client, "/api/v1/disciplinary-history/import/preview", file);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<HistoryImportPreviewDto>>())!.Data!;
    }

    private static async Task<HistoryImportResultDto> ImportAsync(HttpClient client, byte[] file)
    {
        var preview = await PreviewAsync(client, file);
        var response = await PostAsync(client, $"/api/v1/disciplinary-history/import/{preview.BatchId}/confirm", file);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<HistoryImportResultDto>>())!.Data!;
    }

    [Fact]
    public async Task Preview_MatchesTheHistoricoTable_RowByRow()
    {
        using var admin = await AdminAsync();
        var preview = await PreviewAsync(admin, History);
        var row = preview.ToImport.ToDictionary(r => r.Row);
        string[] WarningsOf(int line) => preview.Warnings.Where(w => w.Row == line).Select(w => w.Message).ToArray();

        // Totais: 16 linhas com dados, 14 importadas, 2 rejeitadas, 1 motivo novo.
        preview.Summary.Should().BeEquivalentTo(new HistoryImportSummaryDto(TotalRows: 16, ToImport: 14, Existing: 0, ToUpdate: 0, Rejected: 2, Warnings: 6, NewInfractionTypes: 1, Absent: 0));
        row.Keys.Should().BeEquivalentTo([2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14, 15, 16, 17]);

        // 2: advertência normal
        row[2].Should().Match<HistoryRowDto>(r => r.EmployeeNumber == "50090007" && r.MeasureType == DisciplinaryMeasureType.WrittenWarning);
        // 3: suspensão de 1 dia, período 15/04/2025
        row[3].Should().Match<HistoryRowDto>(r => r.MeasureType == DisciplinaryMeasureType.Suspension
            && r.PeriodStart == new DateOnly(2025, 4, 15) && r.PeriodEnd == new DateOnly(2025, 4, 15) && r.SuspensionDays == 1);
        // 4: suspensão de 3 dias, 02/06/2025 a 04/06/2025
        row[4].Should().Match<HistoryRowDto>(r => r.PeriodStart == new DateOnly(2025, 6, 2) && r.PeriodEnd == new DateOnly(2025, 6, 4) && r.SuspensionDays == 3);
        // 5: advertência por EPI
        row[5].Should().Match<HistoryRowDto>(r => r.EmployeeNumber == "50090006" && r.InfractionName == "FALTA DE UTILIZAÇÃO DO EPI" && !r.NewInfractionType);
        // 6: Nº ADVERTÊNCIA = 1, mas é a 2ª → aviso (planilha 1, recalculado 2)
        WarningsOf(6).Should().ContainSingle().Which.Should().Contain("planilha 1, recalculado 2");
        // 7: suspensão sem dias e sem período → aviso; dias desconhecidos (não zero)
        row[7].Should().Match<HistoryRowDto>(r => r.MeasureType == DisciplinaryMeasureType.Suspension && r.SuspensionDays == null && r.DaysUnknown && r.PeriodStart == null);
        WarningsOf(7).Should().ContainSingle().Which.Should().Contain("sem período");
        // 8: advertência com DIAS = 1 → advertência; aviso; dias ignorados
        row[8].Should().Match<HistoryRowDto>(r => r.MeasureType == DisciplinaryMeasureType.WrittenWarning && r.SuspensionDays == null);
        WarningsOf(8).Should().ContainSingle().Which.Should().Contain("dias foram ignorados");
        // 9: suspensão sem período, com DATA FIM → 12/01/2026 a 14/01/2026
        row[9].Should().Match<HistoryRowDto>(r => r.PeriodStart == new DateOnly(2026, 1, 12) && r.PeriodEnd == new DateOnly(2026, 1, 14) && r.SuspensionDays == 3);
        WarningsOf(9).Should().BeEmpty();
        // 10: motivo novo listado
        preview.NewInfractionTypes.Should().Equal("USO DE CELULAR EM SERVIÇO");
        row[10].NewInfractionType.Should().BeTrue();
        WarningsOf(10).Should().ContainSingle().Which.Should().Contain("Motivo novo");
        // 11: GERÊNCIA = MANUTENÇÃO, funcionário hoje na USGA131 → aviso; gerência da época = USGA132
        row[11].ManagementUnitCode.Should().Be("USGA132");
        WarningsOf(11).Should().ContainSingle().Which.Should().Contain("USGA132").And.Contain("USGA131");
        // 12: matrícula inexistente → rejeitada; 13: cópia da linha 2 → rejeitada
        preview.Rejected.Select(r => (r.Row, r.Message)).Should().BeEquivalentTo(new[]
        {
            ((int?)12, "Funcionário não encontrado — importe a base completa do SAP antes."),
            ((int?)13, "Linha duplicada dentro do arquivo (igual à linha 2)."),
        });
        // 14: justa causa
        row[14].MeasureType.Should().Be(DisciplinaryMeasureType.JustCauseTermination);
        // 15: DATA como texto "15/02/2026"
        row[15].Date.Should().Be(new DateOnly(2026, 2, 15));
        // 16: período com "a" minúsculo
        row[16].Should().Match<HistoryRowDto>(r => r.PeriodStart == new DateOnly(2026, 3, 3) && r.PeriodEnd == new DateOnly(2026, 3, 5) && r.SuspensionDays == 3);
        // 17: DIAS = 3 mas o período tem 5 dias → vale o período; aviso
        row[17].SuspensionDays.Should().Be(5);
        WarningsOf(17).Should().ContainSingle().Which.Should().Contain("vale o período");
        // 18: linha vazia ignorada, sem erro (16 linhas com dados)
        preview.Rejected.Should().NotContain(r => r.Row == 18);
        preview.Warnings.Should().NotContain(w => w.Row == 18);

        // Avisos esperados: linhas 6, 7, 8, 11 e 17, além do motivo novo da linha 10.
        preview.Warnings.Select(w => w.Row).Should().BeEquivalentTo(new int?[] { 6, 7, 8, 10, 11, 17 });

        // Pré-visualização não grava nada.
        await using var scope = Factory.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<SighDbContext>().DisciplinaryCases.AnyAsync(c => c.Kind == DisciplinaryCaseKind.Historical)).Should().BeFalse();
    }

    [Fact]
    public async Task Confirm_ImportsFourteenReadOnlyHistoricalRecords_WithoutDecisionNorApproval()
    {
        using var admin = await AdminAsync();
        var result = await ImportAsync(admin, History);
        result.Imported.Should().Be(14);
        result.InfractionTypesCreated.Should().Be(1);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var historical = await db.DisciplinaryCases.AsNoTracking().Include(c => c.Decisions).Include(c => c.Measures).Include(c => c.Employees).Include(c => c.Occurrences)
            .Where(c => c.Kind == DisciplinaryCaseKind.Historical).ToListAsync();

        historical.Should().HaveCount(14);
        historical.Should().OnlyContain(c => c.Status == DisciplinaryCaseStatus.Completed && c.HistoryImportBatchId == result.BatchId);
        // Nenhum registro histórico tem decisão, decisor ou aprovador; a medida não tem decisão nem quem aplicou.
        historical.Should().OnlyContain(c => c.Decisions.Count == 0);
        historical.SelectMany(c => c.Measures).Should().OnlyContain(m => m.DisciplinaryDecisionId == null && m.AppliedByUserId == null && m.Status == DisciplinaryMeasureStatus.Applied);
        (await db.DisciplinaryDecisions.AnyAsync(d => historical.Select(h => h.Id).Contains(d.DisciplinaryCaseId))).Should().BeFalse();

        var batch = await db.DisciplinaryHistoryImportBatches.SingleAsync(b => b.Id == result.BatchId);
        batch.FileSha256.Should().Be(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(History)).ToLowerInvariant());
        batch.AppliedByUserId.Should().Be(CustomWebApplicationFactory.AdminUserId);

        // Motivo novo criado ativo; justa causa não altera o status do funcionário; gerência da época da linha 11.
        (await db.InfractionTypes.SingleAsync(i => i.Name == "USO DE CELULAR EM SERVIÇO")).IsActive.Should().BeTrue();
        (await db.Employees.SingleAsync(e => e.EmployeeNumber == "50090011")).Status.Should().Be(EmployeeStatus.Active);
        var u132 = await db.ManagementUnits.SingleAsync(m => m.Code == "USGA132");
        var e12 = await db.Employees.SingleAsync(e => e.EmployeeNumber == "50090012");
        historical.Single(c => c.Employees.Any(e => e.EmployeeId == e12.Id)).Employees.Single().ManagementUnitIdAtTime.Should().Be(u132.Id);
        var e00 = await db.Employees.SingleAsync(e => e.EmployeeNumber == "50090000");
        historical.Single(c => c.Employees.Any(e => e.EmployeeId == e00.Id)).Measures.Single().SuspensionDays.Should().BeNull("dias desconhecidos, nunca zero");

        // Somente leitura: o fluxo de medidas recusa, e qualquer operação de domínio também.
        var anyId = historical[0].Id;
        await AccessTestHelpers.CreateUserAsync(Factory, "encarregado@sigh.test", SystemRoles.HrApprover);
        using var approver = await AccessTestHelpers.LoginAsync(Factory, "encarregado@sigh.test");
        (await approver.PostAsync($"/api/v1/disciplinary-records/{anyId}/approve", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = (await approver.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{anyId}"))!.Data!;
        detail.Summary.Status.Should().Be(RecordStatus.Historical);
        detail.Actions.Should().Match<RecordActionsDto>(a => !a.CanApprove && !a.CanReject && !a.CanCorrect && !a.CanCancel);
        detail.Actions.CanAttach.Should().BeTrue("a carta assinada antiga pode ser anexada");
    }

    [Fact]
    public async Task DecisionSupportOf50090007_ShowsOneWarningTwoSuspensionsFourDays_AllHistorical()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, History);
        var id = await ReferenceData.EmployeeIdAsync(Factory, "50090007");

        var support = (await admin.GetFromJsonAsync<Result<EmployeeDecisionSupportDto>>($"/api/v1/employees/{id}/decision-support"))!.Data!;
        support.Warnings.Should().Be(1);
        support.Suspensions.Should().Be(2);
        support.SuspensionDays.Should().Be(4);
        support.HistoricalMeasures.Should().Be(3);
        support.SighMeasures.Should().Be(0);
        support.ByOrigin.Should().BeEquivalentTo(new[]
        {
            new OriginBreakdown("Advertências", 1, 1, 0),
            new OriginBreakdown("Suspensões", 2, 2, 0),
        });
        support.Sources.Should().Equal(MeasureSources.Historical);

        // 50090000: suspensão sem dias → não soma e é sinalizada.
        var e00 = await ReferenceData.EmployeeIdAsync(Factory, "50090000");
        var unknown = (await admin.GetFromJsonAsync<Result<EmployeeDecisionSupportDto>>($"/api/v1/employees/{e00}/decision-support"))!.Data!;
        unknown.Suspensions.Should().Be(1);
        unknown.SuspensionDays.Should().Be(0);
        unknown.SuspensionsWithoutPeriod.Should().Be(1);
    }

    [Fact]
    public async Task ReimportingTheSameFile_CreatesNothingNew()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, History);

        var again = await PreviewAsync(admin, History);
        again.Summary.Should().BeEquivalentTo(new HistoryImportSummaryDto(16, 0, 14, 0, 2, 5, 0, 0),
            "as 14 linhas já existem; os avisos de dados continuam, sem o de motivo novo");
        var result = await ImportAsync(admin, History);
        result.Imported.Should().Be(0);
        result.Updated.Should().Be(0);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.DisciplinaryCases.CountAsync(c => c.Kind == DisciplinaryCaseKind.Historical)).Should().Be(14);
        (await db.InfractionTypes.CountAsync(i => i.Name == "USO DE CELULAR EM SERVIÇO")).Should().Be(1);
    }

    [Fact]
    public async Task ReimportingACorrectedFile_UpdatesTheRecordWithFieldAudit_AndFlagsTheMissingOnes()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, History);

        // Planilha corrigida: linha 17 com o período certo (10 a 12/03 = 3 dias) e linha 11 com GERÊNCIA INDUSTRIAL;
        // a linha 5 some do arquivo.
        var corrected = EditSheet(History, cells =>
        {
            cells("F17", "10/03/2026 À 12/03/2026");
            cells("K11", "INDUSTRIAL");
        }, removeRow: 5);

        var preview = await PreviewAsync(admin, corrected);
        preview.ToUpdate.Select(u => u.Row.Row).Should().BeEquivalentTo([17, 11]);
        preview.ToUpdate.Single(u => u.Row.Row == 17).Changes.Should().BeEquivalentTo(new[]
        {
            new HistoryFieldChange("Fim do período", "14/03/2026", "12/03/2026"),
            new HistoryFieldChange("Dias de suspensão", "5", "3"),
        });
        preview.ToUpdate.Single(u => u.Row.Row == 11).Changes.Should().ContainSingle(c => c.Field == "Gerência da época");
        preview.Absent.Should().ContainSingle(a => a.EmployeeNumber == "50090006" && a.Date == new DateOnly(2025, 5, 20));
        preview.Summary.Absent.Should().Be(1);

        var result = await ImportAsync(admin, corrected);
        result.Updated.Should().Be(2);
        result.Imported.Should().Be(0);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.DisciplinaryCases.CountAsync(c => c.Kind == DisciplinaryCaseKind.Historical)).Should().Be(14, "o ausente nunca é apagado");
        var e02 = await ReferenceData.EmployeeIdAsync(Factory, "50090002");
        var measure = await db.DisciplinaryMeasures.SingleAsync(m => m.EmployeeId == e02);
        measure.SuspensionDays.Should().Be(3);
        var audit = await db.AuditLogs.AsNoTracking().Where(a => a.Entity == "DisciplinaryMeasure" && a.EntityId == measure.Id.ToString() && a.Action == "Updated").ToListAsync();
        audit.Should().ContainSingle().Which.NewValues.Should().Contain("SuspensionDays").And.Contain("EffectiveUntil");
        var e12 = await ReferenceData.EmployeeIdAsync(Factory, "50090012");
        var link = await db.DisciplinaryCaseEmployees.SingleAsync(e => e.EmployeeId == e12);
        (await db.AuditLogs.AnyAsync(a => a.Entity == "DisciplinaryCaseEmployee" && a.EntityId == link.Id.ToString() && a.NewValues!.Contains("ManagementUnitIdAtTime"))).Should().BeTrue();
    }

    [Fact]
    public async Task AfterClosing_TheImportEndpointsAnswer409_AndTheClosureIsAudited()
    {
        using var admin = await AdminAsync();
        var preview = await PreviewAsync(admin, History);

        (await admin.PostAsync("/api/v1/disciplinary-history/import/close", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var status = (await admin.GetFromJsonAsync<Result<HistoryImportStatusDto>>("/api/v1/disciplinary-history/import/status"))!.Data!;
        status.IsClosed.Should().BeTrue();

        foreach (var response in new[]
                 {
                     await PostAsync(admin, "/api/v1/disciplinary-history/import/preview", History),
                     await PostAsync(admin, $"/api/v1/disciplinary-history/import/{preview.BatchId}/confirm", History),
                     await admin.PostAsync("/api/v1/disciplinary-history/import/close", null),
                 })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()
                .Should().Contain("encerrada").And.Contain("History:ImportClosed");
        }

        // Lotes continuam consultáveis.
        (await admin.GetAsync("/api/v1/disciplinary-history/import/batches")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.AuditLogs.AnyAsync(a => a.Action == HistoryAuditActions.ImportClosed && a.UserId == CustomWebApplicationFactory.AdminUserId)).Should().BeTrue();
    }

    [Fact]
    public async Task ReopeningIsOnlyByConfiguration_AppliedAndAuditedAtStartup()
    {
        using var admin = await AdminAsync();
        (await admin.PostAsync("/api/v1/disciplinary-history/import/close", null)).EnsureSuccessStatusCode();

        using var reopened = Factory.WithWebHostBuilder(b => b.UseSetting("History:ImportClosed", "false"));
        _ = reopened.CreateClient(); // startup aplica a configuração

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.HistoryImportStates.SingleAsync()).IsClosed.Should().BeFalse();
        (await db.AuditLogs.AnyAsync(a => a.Action == HistoryAuditActions.ImportReopenedByConfiguration)).Should().BeTrue();
    }

    [Fact]
    public async Task OnlyTheAdministratorImportsHistory()
    {
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        (await PostAsync(registrar, "/api/v1/disciplinary-history/import/preview", History)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        SystemRoles.Find(SystemRoles.Administrator)!.Permissions.Should().Contain(PermissionCodes.DisciplinaryHistory.Import);
        SystemRoles.All.Where(r => r.Name != SystemRoles.Administrator).Should().OnlyContain(r => !r.Permissions.Contains(PermissionCodes.DisciplinaryHistory.Import));
    }

    [Fact]
    public async Task HistoricalRecords_AppearInListsReportsCsvAndTimeline_WithOrigin_AndFollowTheScope()
    {
        using var admin = await AdminAsync();
        var import = await ImportAsync(admin, History);

        async Task<PagedResult<RecordSummaryDto>> List(HttpClient client, string query) =>
            (await client.GetFromJsonAsync<Result<PagedResult<RecordSummaryDto>>>($"/api/v1/disciplinary-records?pageSize=100&{query}"))!.Data!;

        // Filtro Origem.
        (await List(admin, $"origin={(int)RecordOrigin.Historical}")).TotalCount.Should().Be(14);
        (await List(admin, $"origin={(int)RecordOrigin.Sigh}")).TotalCount.Should().Be(0);
        var unknown = (await List(admin, "employee=50090000")).Items.Single();
        unknown.Should().Match<RecordSummaryDto>(r => r.Origin == RecordOrigin.Historical && r.Status == RecordStatus.Historical
            && r.SuspensionDays == null && r.SuspensionDaysUnknown && r.ImportedAt != null && r.MeasureDate == new DateOnly(2025, 9, 1));

        // Relatório de medidas: dias "não informado"; CSV com a coluna Origem.
        var measures = await admin.GetStringAsync("/api/v1/disciplinary-reports/measures?pageSize=100&origin=2");
        var items = JsonDocument.Parse(measures).RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCount(14).And.OnlyContain(i => i.GetProperty("origin").GetString() == "Histórico");
        items.Count(i => i.GetProperty("suspensionDaysUnknown").GetBoolean()).Should().Be(1);
        var csv = await admin.GetStringAsync("/api/v1/disciplinary-reports/cases/export");
        csv.Split('\n')[0].TrimEnd('\r').Should().EndWith("Origem");
        csv.Should().Contain("Histórico");

        // Linha do tempo do funcionário: selo do lote.
        var e07 = await ReferenceData.EmployeeIdAsync(Factory, "50090007");
        var history = await admin.GetStringAsync($"/api/v1/disciplinary-reports/employees/{e07}/history");
        var cases = JsonDocument.Parse(history).RootElement.GetProperty("data").GetProperty("cases").EnumerateArray().ToList();
        cases.Should().HaveCount(3).And.OnlyContain(c => c.GetProperty("origin").GetString() == "Histórico" && c.GetProperty("importedAt").ValueKind != JsonValueKind.Null);

        // Escopo (mesma regra do sprint 6: gerência ATUAL do funcionário). 50090012 tem GERÊNCIA da época = USGA132,
        // mas está hoje na USGA131: o gestor da 132 não o vê; vê a 50090000 (USGA132).
        await AccessTestHelpers.CreateUserAsync(Factory, "g132@sigh.test", SystemRoles.Manager, "USGA132");
        using var g132 = await AccessTestHelpers.LoginAsync(Factory, "g132@sigh.test");
        var seen = await List(g132, "");
        seen.Items.Select(r => r.EmployeeNumber).Should().BeEquivalentTo(["50090000"]);
        var hidden = (await List(admin, "employee=50090012")).Items.Single();
        (await g132.GetAsync($"/api/v1/disciplinary-records/{hidden.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        import.Imported.Should().Be(14);
    }

    /// <summary>Edita células (inline) e remove uma linha da planilha, em memória.</summary>
    private static byte[] EditSheet(byte[] xlsx, Action<Action<string, string>> edits, int? removeRow = null)
    {
        var buffer = new MemoryStream();
        buffer.Write(xlsx);
        using (var doc = SpreadsheetDocument.Open(buffer, true))
        {
            var sheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
            var data = sheet.GetFirstChild<SheetData>()!;
            edits((reference, value) =>
            {
                var rowIndex = uint.Parse(new string(reference.Where(char.IsDigit).ToArray()));
                var row = data.Elements<Row>().Single(r => r.RowIndex! == rowIndex);
                var cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference == reference);
                if (cell is null)
                {
                    cell = new Cell { CellReference = reference };
                    var after = row.Elements<Cell>().LastOrDefault(c => string.CompareOrdinal(c.CellReference!.Value!.PadLeft(4), reference.PadLeft(4)) < 0);
                    if (after is null) row.PrependChild(cell); else row.InsertAfter(cell, after);
                }
                cell.DataType = CellValues.InlineString;
                cell.CellValue = null;
                cell.RemoveAllChildren();
                cell.InlineString = new InlineString(new Text(value));
            });
            if (removeRow is { } r)
                data.Elements<Row>().Single(x => x.RowIndex! == (uint)r).RemoveAllChildren<Cell>();
            sheet.Save();
        }
        return buffer.ToArray();
    }
}
