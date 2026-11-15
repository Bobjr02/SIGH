using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Audit;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Filtros legíveis da auditoria: busca pelo número da medida (MED-…) ou pela matrícula, e seletor de
/// usuário por nome (no lugar dos campos de ID técnico).
/// </summary>
public class AuditReferenceSearchTests : IntegrationTestBase
{
    public AuditReferenceSearchTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Reference_ByRecordNumber_ReturnsTheRecordAndItsSubentities_AndUnknownReturnsNothing()
    {
        await ReferenceData.LoadEmployeesAsync(Factory);
        var employee = await ReferenceData.EmployeeIdAsync(Factory, "50090006");
        var infraction = await ReferenceData.InfractionTypeAsync(Factory);
        var today = BusinessCalendar.Today(Factory.Clock.UtcNow);
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        await AccessTestHelpers.CreateUserAsync(Factory, "juridico@sigh.test", SystemRoles.Legal);

        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        var first = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(employee, infraction, today.AddDays(-3)));
        await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(employee, infraction, today.AddDays(-1)));

        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");
        var page = (await legal.GetFromJsonAsync<Result<PagedResult<AuditLogDto>>>(
            $"/api/v1/audit-logs?reference={first.Summary.CaseNumber.ToLowerInvariant()}&pageSize=200"))!.Data!;
        page.Items.Should().NotBeEmpty();
        page.Items.Should().Contain(a => a.EntityId == first.Summary.Id.ToString());
        page.Items.Should().Contain(a => a.Entity == "DisciplinaryDecision", "as subentidades da medida entram na busca");

        var trail = (await legal.GetFromJsonAsync<Result<PagedResult<AuditLogDto>>>($"/api/v1/disciplinary-cases/{first.Summary.Id}/audit-trail?pageSize=200"))!.Data!;
        page.TotalCount.Should().Be(trail.TotalCount, "é a mesma linha do tempo da medida");

        var none = (await legal.GetFromJsonAsync<Result<PagedResult<AuditLogDto>>>("/api/v1/audit-logs?reference=MED-0000-NAOEXISTE"))!.Data!;
        none.TotalCount.Should().Be(0, "referência sem correspondência nunca devolve tudo");
    }

    [Fact]
    public async Task Reference_ByEmployeeNumber_ReturnsTheEmployeeChanges()
    {
        await ReferenceData.LoadEmployeesAsync(Factory);
        var employee = await ReferenceData.EmployeeIdAsync(Factory, "50090006");
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);

        var page = (await admin.GetFromJsonAsync<Result<PagedResult<AuditLogDto>>>("/api/v1/audit-logs?reference=50090006&pageSize=200"))!.Data!;
        page.Items.Should().NotBeEmpty().And.OnlyContain(a => a.EntityId == employee.ToString());
    }

    [Fact]
    public async Task UserSelector_FindsByName_AndRequiresAuditView()
    {
        await AccessTestHelpers.CreateUserAsync(Factory, "juridico@sigh.test", SystemRoles.Legal);
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);

        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");
        var users = (await legal.GetFromJsonAsync<Result<List<AuditUserOptionDto>>>("/api/v1/audit-logs/users?search=auxil"))!.Data!;
        users.Should().ContainSingle(u => u.Email == "auxiliar@sigh.test");

        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        (await registrar.GetAsync("/api/v1/audit-logs/users?search=a")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
