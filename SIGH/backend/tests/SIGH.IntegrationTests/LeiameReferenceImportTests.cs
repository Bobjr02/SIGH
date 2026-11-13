using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Importação com os arquivos FICTÍCIOS do pacote de referência (SIGH/docs/referencias, LEIAME seção 2),
/// conferindo a tabela "Resultado esperado ao importar depois da carga inicial" linha a linha.
/// </summary>
public class LeiameReferenceImportTests : IntegrationTestBase
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Referencias");
    private static byte[] InitialLoad => File.ReadAllBytes(Path.Combine(FixtureDir, "funcionarios_carga_inicial.xlsx"));
    private static byte[] Update => File.ReadAllBytes(Path.Combine(FixtureDir, "funcionarios_atualizacao.xlsx"));

    private static readonly string[] SensitiveMarkers = ["FICTICIA", "FICTICIO", "00000-0", "RUA FICTICIA"];

    public LeiameReferenceImportTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task InitialLoad_Creates40Employees_WithGroupAndTeamForField()
    {
        using var admin = await AdminAsync();

        var preview = await PreviewAsync(admin, InitialLoad);

        preview.Summary.TotalRows.Should().Be(40);
        preview.Summary.Created.Should().Be(40);
        preview.Errors.Should().BeEmpty();
        preview.Warnings.Should().NotContain(w => w.Row == null && w.Message.Contains("não encontrada"),
            "o arquivo do pacote tem todas as colunas da lista fechada");
        (await ConfirmAsync(admin, preview.BatchId, InitialLoad)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var employees = await db.Employees.AsNoTracking().ToListAsync();
        employees.Should().HaveCount(40);

        var field = employees.Where(e => e.Group == EmployeeGroup.Field).ToList();
        field.Should().HaveCount(26, "26 linhas da carga têm Grupo_Emp = Campo");
        field.Should().OnlyContain(e => !string.IsNullOrEmpty(e.Team), "Campo tem Descrição Turma");
        employees.Single(e => e.EmployeeNumber == "50090001").Team.Should().Be("001-CORTE DE CANA", "a turma vem de \"Descrição Turma\", não do código \"Turma\"");
        employees.Where(e => e.Group == EmployeeGroup.Industry).Should().HaveCount(14);
        employees.Should().OnlyContain(e => e.Group != null, "Grupo_Emp vem preenchido em todas as linhas");

        employees.Single(e => e.EmployeeNumber == "50090005").Cpf.Should().Be("01687731390");
    }

    [Fact]
    public async Task Update_MatchesTheLeiameTable_RowByRow()
    {
        using var admin = await AdminAsync();
        await ImportAsync(admin, InitialLoad);

        var preview = await PreviewAsync(admin, Update);

        // 50090010 — ausente do arquivo → marcado como desligado (nunca apagado)
        preview.ToTerminate.Select(c => c.EmployeeNumber).Should().Equal("50090010");

        // 50090001 — Status Ativo → Afastado → atualizado
        preview.Updated.Single(c => c.EmployeeNumber == "50090001").Changes
            .Should().ContainSingle().Which.Should().BeEquivalentTo(new FieldChange("Situação", "Ativo", "Afastado"));

        // 50090002 — nome corrigido → atualizado
        preview.Updated.Single(c => c.EmployeeNumber == "50090002").Changes
            .Should().ContainSingle().Which.Should().BeEquivalentTo(
                new FieldChange("Nome", "RITA ANTONIO ALVES BARBOSA TESTE", "RITA ANTONIO ALVES BARBOSA TESTE JUNIOR"));

        // 59999998 — novo → criado
        preview.Created.Select(c => c.EmployeeNumber).Should().Equal("59999998");

        // 59999999 — CPF inválido; 59999997 — nome vazio; 59999996 — CPF igual ao da 50090007: linhas rejeitadas com erro
        preview.Errors.Select(e => e.EmployeeNumber).Should().BeEquivalentTo("59999999", "59999997", "59999996");
        preview.Errors.Single(e => e.EmployeeNumber == "59999999").Message.Should().Contain("CPF inválido");
        preview.Errors.Single(e => e.EmployeeNumber == "59999997").Message.Should().Contain("Nome obrigatório");
        preview.Errors.Single(e => e.EmployeeNumber == "59999996").Message.Should().Contain("50090007");
        preview.Errors.Should().OnlyContain(e => e.Row > 1);
        preview.Errors.Should().NotContain(e => Regex.IsMatch(e.Message, @"\d{11}"), "o erro nunca traz o CPF");

        // 50090000 — setor mudou (13205 SERRALHARIA → 13110 MANUTENCAO INDUSTRIAL) → atualizado
        preview.Updated.Single(c => c.EmployeeNumber == "50090000").Changes
            .Should().ContainSingle().Which.Should().BeEquivalentTo(new FieldChange("Setor (Uni.Org)", "13205", "13110"));

        // 50090005 (CPF começa com zero), 50090007 (dona do CPF repetido) e demais: sem mudança
        // (43 linhas = 39 existentes + 4 novas; 39 - 3 alteradas = 36 sem mudança)
        preview.Unchanged.Should().Contain(["50090005", "50090007"]);
        preview.Summary.Should().BeEquivalentTo(new ImportSummaryDto(
            TotalRows: 43, Created: 1, Updated: 3, Terminated: 0, TerminatedByAbsence: 1, Reactivated: 0, Unchanged: 36,
            Errors: 3, Warnings: preview.Summary.Warnings));
        preview.RequiresMassTerminationConfirmation.Should().BeFalse("1 ausente em 40 ativos = 2,5%");

        (await ConfirmAsync(admin, preview.BatchId, Update)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var all = await db.Employees.AsNoTracking().ToDictionaryAsync(e => e.EmployeeNumber);

        all.Should().HaveCount(41, "40 da carga + 59999998; ninguém é apagado");
        all["50090010"].Status.Should().Be(EmployeeStatus.Terminated);
        all["50090010"].TerminationDate.Should().NotBeNull();
        all["50090001"].Status.Should().Be(EmployeeStatus.OnLeave);
        all["50090002"].FullName.Should().Be("RITA ANTONIO ALVES BARBOSA TESTE JUNIOR");
        all.Should().ContainKey("59999998");
        all.Should().NotContainKeys("59999999", "59999997", "59999996");
        all["50090005"].Cpf.Should().Be("01687731390").And.StartWith("0");
        all["50090007"].FullName.Should().Be("FRANCISCA PEDRO COSTA SOUZA TESTE");
        (await db.Departments.SingleAsync(d => d.Id == all["50090000"].DepartmentId)).Name.Should().Be("MANUTENCAO INDUSTRIAL");

        // Auditoria dos campos atualizados.
        var audits = await db.AuditLogs.AsNoTracking().Where(a => a.Entity == "Employee" && a.Action == "Updated").ToListAsync();
        audits.Should().Contain(a => a.EntityId == all["50090001"].Id.ToString() && a.NewValues!.Contains("\"Status\":\"OnLeave\""));
        audits.Should().Contain(a => a.EntityId == all["50090002"].Id.ToString() && a.NewValues!.Contains("TESTE JUNIOR"));
        audits.Should().Contain(a => a.EntityId == all["50090010"].Id.ToString() && a.NewValues!.Contains("\"Status\":\"Terminated\""));
        audits.Should().Contain(a => a.EntityId == all["50090000"].Id.ToString() && a.NewValues!.Contains("DepartmentId"),
            "a troca de setor é auditada com o campo alterado");
        audits.Should().HaveCount(4, "só as 4 matrículas alteradas geram auditoria");
    }

    [Fact]
    public async Task SensitiveColumns_OfThePackageFiles_NeverReachTheDatabase()
    {
        // O teste só prova algo se os arquivos tiverem os marcadores.
        foreach (var marker in SensitiveMarkers)
            SheetText(InitialLoad).Should().Contain(marker);

        using var admin = await AdminAsync();
        await ImportAsync(admin, InitialLoad);
        await ImportAsync(admin, Update);

        await using var connection = new SqlConnection(await SqlServerTestDatabase.GetConnectionStringAsync());
        await connection.OpenAsync();
        var columns = new List<(string Table, string Column)>();
        await using (var cmd = new SqlCommand(
            "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE DATA_TYPE IN ('nvarchar','varchar','nchar','char','ntext','text') AND TABLE_SCHEMA = 'dbo'", connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) columns.Add((reader.GetString(0), reader.GetString(1)));

        // GUIDs são removidos antes: "00000-0" aparece dentro de ids como 10000000-0000-...
        var guid = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        var scanned = 0;
        foreach (var (table, column) in columns)
        {
            await using var read = new SqlCommand($"SELECT CAST([{column}] AS nvarchar(max)) FROM [{table}] WHERE [{column}] IS NOT NULL", connection);
            await using var values = await read.ExecuteReaderAsync();
            while (await values.ReadAsync())
            {
                scanned++;
                var text = guid.Replace(values.GetString(0), string.Empty);
                foreach (var marker in SensitiveMarkers)
                    text.Should().NotContain(marker, $"\"{marker}\" não pode estar em {table}.{column}");
            }
        }
        scanned.Should().BeGreaterThan(500, "a varredura precisa ter lido os dados importados");
    }

    [Fact]
    public async Task MissingGrupoEmp_ShowsAFileLevelWarning_AndLeavesGroupEmpty()
    {
        using var admin = await AdminAsync();
        var file = WithoutColumn(InitialLoad, "Grupo_Emp");

        var preview = await PreviewAsync(admin, file);

        preview.Warnings.Should().Contain(w => w.Row == null
            && w.Message == "Coluna \"Grupo_Emp\" não encontrada; o campo Grupo ficará vazio para todos.");
        preview.Summary.Created.Should().Be(40);
        (await ConfirmAsync(admin, preview.BatchId, file)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<SighDbContext>().Employees.AnyAsync(e => e.Group != null)).Should().BeFalse();
    }

    // ---- helpers: edição em memória dos arquivos do pacote (os originais nunca mudam) ----

    private static string SheetText(byte[] xlsx)
    {
        using var doc = SpreadsheetDocument.Open(new MemoryStream(xlsx), false);
        return string.Join("\n", doc.WorkbookPart!.WorksheetParts.SelectMany(p => p.Worksheet.Descendants<Text>()).Select(t => t.Text));
    }

    private static (Dictionary<string, string> ColumnByHeader, SheetData Data) Open(SpreadsheetDocument doc)
    {
        var data = doc.WorkbookPart!.WorksheetParts.First().Worksheet.GetFirstChild<SheetData>()!;
        var header = data.Elements<Row>().First();
        var columns = new Dictionary<string, string>();
        foreach (var cell in header.Elements<Cell>())
            columns.TryAdd(cell.InnerText, Regex.Match(cell.CellReference!.Value!, "^[A-Z]+").Value);
        return (columns, data);
    }

    private static byte[] WithoutColumn(byte[] xlsx, string headerName)
    {
        var buffer = new MemoryStream();
        buffer.Write(xlsx);
        using (var doc = SpreadsheetDocument.Open(buffer, true))
        {
            var (_, data) = Open(doc);
            // Troca o cabeçalho por um nome fora da lista: a coluna deixa de existir para o importador.
            var cell = data.Elements<Row>().First().Elements<Cell>().Single(c => c.InnerText == headerName);
            cell.DataType = CellValues.InlineString;
            cell.CellValue = null;
            cell.RemoveAllChildren();
            cell.InlineString = new InlineString(new Text("Coluna removida"));
            doc.WorkbookPart!.WorksheetParts.First().Worksheet.Save();
        }
        return buffer.ToArray();
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = Factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, byte[] file, bool? confirm = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(file), "file", "funcionarios.xlsx");
        if (confirm is { } c) form.Add(new StringContent(c ? "true" : "false"), "confirmMassTermination");
        return await client.PostAsync(url, form);
    }

    private static async Task<EmployeeImportPreviewDto> PreviewAsync(HttpClient client, byte[] file)
    {
        var response = await PostAsync(client, "/api/v1/employees/import/preview", file);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<EmployeeImportPreviewDto>>())!.Data!;
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid batchId, byte[] file) =>
        PostAsync(client, $"/api/v1/employees/import/{batchId}/confirm", file, confirm: false);

    private static async Task ImportAsync(HttpClient client, byte[] file)
    {
        var preview = await PreviewAsync(client, file);
        var response = await PostAsync(client, $"/api/v1/employees/import/{preview.BatchId}/confirm", file, confirm: true);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
