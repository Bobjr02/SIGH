using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Persistence.Context;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>
/// Dados de referência dos testes de medidas e de escopo: a carga inicial FICTÍCIA do pacote
/// (USGA131: 50090006, 50090012...; USGA132: 50090000...; USGA141: 26 funcionários) e um motivo.
/// </summary>
public static class ReferenceData
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Referencias");

    public static byte[] InitialLoad => File.ReadAllBytes(Path.Combine(FixtureDir, "funcionarios_carga_inicial.xlsx"));

    /// <summary>Importa a carga inicial pela API (preview + confirm) com o admin da fixture.</summary>
    public static async Task LoadEmployeesAsync(CustomWebApplicationFactory factory)
    {
        using var admin = factory.CreateClient();
        admin.Timeout = TimeSpan.FromMinutes(2);
        var (login, _) = await AuthTestHelpers.LoginAsync(admin, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        admin.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);

        var preview = await PostFileAsync(admin, "/api/v1/employees/import/preview", InitialLoad);
        preview.StatusCode.Should().Be(HttpStatusCode.OK, await preview.Content.ReadAsStringAsync());
        var batchId = JsonDocument.Parse(await preview.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("batchId").GetGuid();

        var confirm = await PostFileAsync(admin, $"/api/v1/employees/import/{batchId}/confirm", InitialLoad, confirm: true);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK, await confirm.Content.ReadAsStringAsync());
    }

    public static async Task<Guid> EmployeeIdAsync(CustomWebApplicationFactory factory, string employeeNumber)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SighDbContext>().Employees
            .Where(e => e.EmployeeNumber == employeeNumber).Select(e => e.Id).SingleAsync();
    }

    public static async Task<Guid> InfractionTypeAsync(CustomWebApplicationFactory factory, string code = "FALTA", string name = "Falta injustificada")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var existing = await db.InfractionTypes.Where(i => i.Code == code).Select(i => (Guid?)i.Id).FirstOrDefaultAsync();
        if (existing is { } id)
            return id;

        var type = InfractionType.Create(code, name, InfractionSeverity.Moderate);
        db.InfractionTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    public static RecordInput Warning(Guid employeeId, Guid infractionTypeId, DateOnly occurrence, string description = "Faltou ao turno sem justificativa.") =>
        new(employeeId, infractionTypeId, DisciplinaryMeasureType.WrittenWarning, [occurrence], occurrence, null, null, description);

    public static async Task<RecordDetailDto> RegisterAsync(HttpClient client, RecordInput input)
    {
        var response = await client.PostAsJsonAsync("/api/v1/disciplinary-records", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<RecordSaveResult>>())!.Data!.Record!;
    }

    private static async Task<HttpResponseMessage> PostFileAsync(HttpClient client, string url, byte[] file, bool? confirm = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(file), "file", "funcionarios.xlsx");
        if (confirm is { } c)
            form.Add(new StringContent(c ? "true" : "false"), "confirmMassTermination");
        return await client.PostAsync(url, form);
    }
}
