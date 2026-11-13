using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Api.Contracts.Disciplinary;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.Records;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Entities;
using SIGH.Domain.Notifications.Entities;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests.Scope;

/// <summary>
/// Para CADA rota GET com escopo: o gestor da USGA132 NÃO enxerga o funcionário 50090006 da USGA131
/// (lista sem o registro; detalhe 404, nunca 403), e o gestor da USGA131 (controle positivo) enxerga.
/// </summary>
public class ScopeEnforcementTests : IntegrationTestBase
{
    public ScopeEnforcementTests(CustomWebApplicationFactory factory) : base(factory) { }

    /// <summary>Cenário: medidas de funcionários da USGA131 e da USGA132, gestores de cada uma.</summary>
    public sealed class Scenario
    {
        public Guid Employee131 { get; init; }
        public Guid Record131 { get; init; }
        public string Record131Number { get; init; } = string.Empty;
        public Guid Evidence131 { get; init; }
    }

    private delegate Task<(HttpStatusCode Status, bool Sees)> Probe(HttpClient client, Scenario s);

    private sealed record ScopeCase(Probe Probe, bool PositiveNeedsFullScope = false);

    private static async Task<(HttpStatusCode, string)> GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static Probe Contains(Func<Scenario, string> url, Func<Scenario, string> marker) => async (client, s) =>
    {
        var (status, body) = await GetAsync(client, url(s));
        return (status, status == HttpStatusCode.OK && body.Contains(marker(s), StringComparison.Ordinal));
    };

    private static Probe Found(Func<Scenario, string> url) => async (client, s) =>
    {
        var (status, _) = await GetAsync(client, url(s));
        return (status, status == HttpStatusCode.OK);
    };

    private static Probe Number(Func<Scenario, string> url, Func<JsonElement, int> read, int seenWhenAtLeast) => async (client, s) =>
    {
        var (status, body) = await GetAsync(client, url(s));
        return (status, status == HttpStatusCode.OK && read(JsonDocument.Parse(body).RootElement.GetProperty("data")) >= seenWhenAtLeast);
    };

    /// <summary>Um caso por rota com escopo (conferido por RouteScopeCoverageTests).</summary>
    internal static readonly IReadOnlyDictionary<string, object> Cases = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["/api/v1/employees"] = new ScopeCase(Contains(_ => "/api/v1/employees?pageSize=100", _ => "50090006")),
        ["/api/v1/employees/{id:guid}"] = new ScopeCase(Found(s => $"/api/v1/employees/{s.Employee131}")),
        ["/api/v1/employees/{id:guid}/decision-support"] = new ScopeCase(Found(s => $"/api/v1/employees/{s.Employee131}/decision-support")),
        ["/api/v1/employees/filter-options"] = new ScopeCase(Contains(_ => "/api/v1/employees/filter-options", _ => "USGA131")),

        ["/api/v1/disciplinary-records"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-records?pageSize=100", s => s.Record131.ToString())),
        ["/api/v1/disciplinary-records/{id:guid}"] = new ScopeCase(Found(s => $"/api/v1/disciplinary-records/{s.Record131}")),
        ["/api/v1/disciplinary-records/options"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-records/options", _ => "USGA131")),
        // Só a medida pendente da USGA131 conta; a da USGA132 já foi aprovada.
        ["/api/v1/disciplinary-records/pending-count"] = new ScopeCase(Number(_ => "/api/v1/disciplinary-records/pending-count", d => d.GetInt32(), 1)),

        ["/api/v1/disciplinary-cases"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-cases?pageSize=100", s => s.Record131.ToString())),
        ["/api/v1/disciplinary-cases/{id:guid}"] = new ScopeCase(Found(s => $"/api/v1/disciplinary-cases/{s.Record131}")),
        ["/api/v1/disciplinary-cases/{id:guid}/audit-trail"] = new ScopeCase(Found(s => $"/api/v1/disciplinary-cases/{s.Record131}/audit-trail")),
        ["/api/v1/disciplinary-cases/{caseId:guid}/evidences/{evidenceId:guid}/file"] =
            new ScopeCase(Found(s => $"/api/v1/disciplinary-cases/{s.Record131}/evidences/{s.Evidence131}/file")),

        // Dashboard: o gestor da 131 vê as 2 medidas da 131; o da 132, só a dele.
        ["/api/v1/disciplinary-reports/dashboard"] = new ScopeCase(Number(_ => "/api/v1/disciplinary-reports/dashboard", d => d.GetProperty("totalCases").GetInt32(), 2)),
        ["/api/v1/disciplinary-reports/cases"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-reports/cases?pageSize=100", s => s.Record131.ToString())),
        ["/api/v1/disciplinary-reports/cases/export"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-reports/cases/export", s => s.Record131Number)),
        ["/api/v1/disciplinary-reports/measures"] = new ScopeCase(Contains(_ => "/api/v1/disciplinary-reports/measures?pageSize=100", s => s.Employee131.ToString())),
        ["/api/v1/disciplinary-reports/employees/{employeeId:guid}/history"] = new ScopeCase(Found(s => $"/api/v1/disciplinary-reports/employees/{s.Employee131}/history")),

        ["/api/v1/notifications"] = new ScopeCase(Contains(_ => "/api/v1/notifications", _ => "Aviso da medida 131")),
        ["/api/v1/notifications/unread"] = new ScopeCase(Contains(_ => "/api/v1/notifications/unread", _ => "Aviso da medida 131")),
        // Cada gestor tem 2 notificações: uma geral e uma da medida da 131.
        ["/api/v1/notifications/unread/count"] = new ScopeCase(Number(_ => "/api/v1/notifications/unread/count", d => d.GetInt32(), 2)),

        // Trilha geral: só com escopo total (o controle positivo é o Jurídico).
        ["/api/v1/audit-logs"] = new ScopeCase(Contains(_ => "/api/v1/audit-logs?pageSize=200", s => s.Record131.ToString()), PositiveNeedsFullScope: true),
    };

    public static IEnumerable<object[]> ScopedRoutes() => Cases.Keys.Order(StringComparer.Ordinal).Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(ScopedRoutes))]
    public async Task ManagerOfUsga132_DoesNotSeeAnEmployeeOfUsga131(string route)
    {
        var scenario = await BuildScenarioAsync();
        var scopeCase = (ScopeCase)Cases[route];

        using var positive = await AccessTestHelpers.LoginAsync(Factory, scopeCase.PositiveNeedsFullScope ? "juridico@sigh.test" : "gestor131@sigh.test");
        var (positiveStatus, positiveSees) = await scopeCase.Probe(positive, scenario);
        positiveStatus.Should().Be(HttpStatusCode.OK, $"controle positivo de {route}");
        positiveSees.Should().BeTrue($"o controle positivo deve enxergar a USGA131 em {route}");

        using var manager132 = await AccessTestHelpers.LoginAsync(Factory, "gestor132@sigh.test");
        var (status, sees) = await scopeCase.Probe(manager132, scenario);
        sees.Should().BeFalse($"o gestor da USGA132 não pode enxergar a USGA131 em {route}");
        status.Should().BeOneOf([HttpStatusCode.OK, HttpStatusCode.NotFound], "fora do escopo: lista sem o registro ou 404, nunca 403");
    }

    [Fact]
    public async Task HistoryFollowsTheEmployee_WhenTheEmployeeMovesToAnotherManagementUnit()
    {
        var scenario = await BuildScenarioAsync();
        using var manager132 = await AccessTestHelpers.LoginAsync(Factory, "gestor132@sigh.test");
        (await manager132.GetAsync($"/api/v1/disciplinary-records/{scenario.Record131}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var employee = await db.Employees.SingleAsync(e => e.Id == scenario.Employee131);
            var u132 = await db.ManagementUnits.SingleAsync(m => m.Code == "USGA132");
            employee.ChangeOrganizationalAssignment(employee.JobTitleId, u132.Id, null);
            await db.SaveChangesAsync();
        }

        var detail = await manager132.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{scenario.Record131}");
        detail!.Data!.Summary.ManagementUnitCode.Should().Be("USGA132", "a visibilidade usa a gerência de HOJE");
        detail.Data.ManagementUnitCodeAtTime.Should().Be("USGA131", "a gerência da época fica gravada para os relatórios históricos");

        using var manager131 = await AccessTestHelpers.LoginAsync(Factory, "gestor131@sigh.test");
        (await manager131.GetAsync($"/api/v1/disciplinary-records/{scenario.Record131}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ManagerOfTwoUnits_SeesBoth_AndAnExpiredUnitLinkDoesNotCount()
    {
        var scenario = await BuildScenarioAsync();
        var now = Factory.Clock.UtcNow;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == "gestor132@sigh.test");
            var u131 = await db.ManagementUnits.SingleAsync(m => m.Code == "USGA131");
            db.UserManagementUnits.Add(new UserManagementUnit { UserId = user.Id, ManagementUnitId = u131.Id, ValidUntil = now.AddMinutes(30) });
            await db.SaveChangesAsync();
        }

        using var manager = await AccessTestHelpers.LoginAsync(Factory, "gestor132@sigh.test");
        (await manager.GetAsync($"/api/v1/disciplinary-records/{scenario.Record131}")).StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31));
        using var later = await AccessTestHelpers.LoginAsync(Factory, "gestor132@sigh.test");
        (await later.GetAsync($"/api/v1/disciplinary-records/{scenario.Record131}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    private async Task<Scenario> BuildScenarioAsync()
    {
        await ReferenceData.LoadEmployeesAsync(Factory);
        var e131 = await ReferenceData.EmployeeIdAsync(Factory, "50090006");
        var e131b = await ReferenceData.EmployeeIdAsync(Factory, "50090012");
        var e132 = await ReferenceData.EmployeeIdAsync(Factory, "50090000");
        var infraction = await ReferenceData.InfractionTypeAsync(Factory);
        var today = BusinessCalendar.Today(Factory.Clock.UtcNow);

        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        await AccessTestHelpers.CreateUserAsync(Factory, "encarregado@sigh.test", SystemRoles.HrApprover);
        await AccessTestHelpers.CreateUserAsync(Factory, "juridico@sigh.test", SystemRoles.Legal);

        // Gestores: perfil Gestor real + um perfil de teste SEM escopo total que libera as rotas que o
        // Gestor não tem (processo legado, auditoria, notificações e o contador de aprovação),
        // para que cada rota com escopo seja exercitada por um usuário limitado às próprias gerências.
        var extras = await CreateRoleAsync("IntegrationTests.ScopedExtras",
            PermissionCodes.DisciplinaryCases.View, PermissionCodes.Audit.View, PermissionCodes.Notifications.View,
            PermissionCodes.Notifications.Read, PermissionCodes.DisciplinaryRecords.Approve);
        var g131 = await AccessTestHelpers.CreateUserAsync(Factory, "gestor131@sigh.test", [new RoleGrant(SystemRoles.Manager), new RoleGrant(extras)], "USGA131");
        var g132 = await AccessTestHelpers.CreateUserAsync(Factory, "gestor132@sigh.test", [new RoleGrant(SystemRoles.Manager), new RoleGrant(extras)], "USGA132");

        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        using var approver = await AccessTestHelpers.LoginAsync(Factory, "encarregado@sigh.test");

        var r131 = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(e131, infraction, today.AddDays(-2)));
        await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(e131b, infraction, today.AddDays(-1)) with { MeasureType = DisciplinaryMeasureType.VerbalWarning });
        var r132 = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(e132, infraction, today.AddDays(-3)));
        foreach (var id in new[] { r131.Summary.Id, r132.Summary.Id })
            (await approver.PostAsync($"/api/v1/disciplinary-records/{id}/approve", null)).EnsureSuccessStatusCode();

        var created = await registrar.PostAsJsonAsync($"/api/v1/disciplinary-cases/{r131.Summary.Id}/evidences",
            new AddEvidenceHttpRequest(EvidenceType.Document, "Carta assinada", DateTimeOffset.UtcNow));
        created.EnsureSuccessStatusCode();
        var evidenceId = (await created.Content.ReadFromJsonAsync<Result<AddEvidenceResponse>>())!.Data!.Id;
        using (var form = new MultipartFormDataContent())
        {
            form.Add(new ByteArrayContent(PdfBytes), "file", "carta.pdf");
            (await registrar.PostAsync($"/api/v1/disciplinary-cases/{r131.Summary.Id}/evidences/{evidenceId}/file", form)).EnsureSuccessStatusCode();
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            // Os avisos automáticos do fluxo (os gestores aprovam pelo perfil de teste) ficam lidos: o contador
            // de não lidas mede só as duas notificações abaixo.
            foreach (var automatic in await db.Notifications.Where(n => n.UserId == g131.Id || n.UserId == g132.Id).ToListAsync())
                automatic.MarkAsRead(DateTimeOffset.UtcNow);
            foreach (var gestor in new[] { g131.Id, g132.Id })
            {
                db.Notifications.Add(Notification.Create(DatabaseSeeder.DefaultCompanyId, gestor, "Aviso da medida 131", "Medida da USGA131.", disciplinaryCaseId: r131.Summary.Id));
                db.Notifications.Add(Notification.Create(DatabaseSeeder.DefaultCompanyId, gestor, "Aviso geral", "Sem medida vinculada."));
            }
            await db.SaveChangesAsync();
        }

        return new Scenario { Employee131 = e131, Record131 = r131.Summary.Id, Record131Number = r131.Summary.CaseNumber, Evidence131 = evidenceId };
    }

    private async Task<string> CreateRoleAsync(string name, params string[] permissions)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == name);
        if (role is null)
        {
            role = new Role(Guid.NewGuid()) { Name = name, Description = "Somente testes" };
            db.Roles.Add(role);
            foreach (var permission in await db.Permissions.Where(p => permissions.Contains(p.Code)).ToListAsync())
                db.RolePermissions.Add(new RolePermission(Guid.NewGuid()) { RoleId = role.Id, PermissionId = permission.Id });
            await db.SaveChangesAsync();
        }
        return name;
    }
}
