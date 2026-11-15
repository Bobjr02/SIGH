using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Página Relatórios e Painel sobre as medidas: "Aplicado por" (quem aprovou), vigência só na suspensão,
/// distribuição com rótulos em português, CSV com cabeçalhos em português e indicadores do painel.
/// </summary>
public class RecordReportsTests : IntegrationTestBase
{
    public RecordReportsTests(CustomWebApplicationFactory factory) : base(factory) { }

    private Guid _employee;
    private Guid _otherEmployee;
    private Guid _infraction;
    private DateOnly _today;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ReferenceData.LoadEmployeesAsync(Factory);
        _employee = await ReferenceData.EmployeeIdAsync(Factory, "50090006");
        _otherEmployee = await ReferenceData.EmployeeIdAsync(Factory, "50090000");
        _infraction = await ReferenceData.InfractionTypeAsync(Factory);
        _today = BusinessCalendar.Today(Factory.Clock.UtcNow);
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        await AccessTestHelpers.CreateUserAsync(Factory, "encarregado@sigh.test", SystemRoles.HrApprover);
        await AccessTestHelpers.CreateUserAsync(Factory, "juridico@sigh.test", SystemRoles.Legal);
    }

    /// <summary>Uma advertência aprovada, uma suspensão aprovada (2 dias) e uma advertência pendente.</summary>
    private async Task<(RecordDetailDto Warning, RecordDetailDto Suspension, RecordDetailDto Pending)> ScenarioAsync()
    {
        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        using var approver = await AccessTestHelpers.LoginAsync(Factory, "encarregado@sigh.test");

        var warning = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-5)));
        var day = _today.AddDays(-4);
        var suspension = await ReferenceData.RegisterAsync(registrar,
            new RecordInput(_otherEmployee, _infraction, DisciplinaryMeasureType.Suspension, [day], day, day.AddDays(1), day.AddDays(2), "Reincidência."));
        var pending = await ReferenceData.RegisterAsync(registrar,
            ReferenceData.Warning(_employee, _infraction, _today.AddDays(-1)) with { MeasureType = DisciplinaryMeasureType.VerbalWarning });

        foreach (var id in new[] { warning.Summary.Id, suspension.Summary.Id })
            (await approver.PostAsync($"/api/v1/disciplinary-records/{id}/approve", null)).EnsureSuccessStatusCode();

        return (warning, suspension, pending);
    }

    [Fact]
    public async Task ApprovedRecord_ShowsTheApproverAsAppliedBy_AndTheMeasureKeepsAppliedByUserId()
    {
        var (warning, suspension, pending) = await ScenarioAsync();
        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");

        var page = (await legal.GetFromJsonAsync<Result<PagedResult<RecordSummaryDto>>>("/api/v1/disciplinary-records?pageSize=50"))!.Data!;
        var approvedWarning = page.Items.Single(i => i.Id == warning.Summary.Id);
        approvedWarning.AppliedByName.Should().Be("encarregado", "a aprovação aplica a medida: quem aprovou é quem aplicou");
        approvedWarning.ApprovedAt.Should().NotBeNull();
        approvedWarning.DaysToApproval.Should().Be(0);
        approvedWarning.SuspensionStart.Should().BeNull("advertência não tem vigência");
        approvedWarning.SuspensionEnd.Should().BeNull();

        var approvedSuspension = page.Items.Single(i => i.Id == suspension.Summary.Id);
        approvedSuspension.SuspensionStart.Should().Be(_today.AddDays(-3));
        approvedSuspension.SuspensionEnd.Should().Be(_today.AddDays(-2));
        approvedSuspension.SuspensionDays.Should().Be(2);

        var stillPending = page.Items.Single(i => i.Id == pending.Summary.Id);
        stillPending.AppliedByName.Should().BeNull();
        stillPending.DaysToApproval.Should().BeNull();

        // Na origem: a medida aprovada grava AppliedByUserId = aprovador.
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var approverId = await db.Users.Where(u => u.Email == "encarregado@sigh.test").Select(u => u.Id).SingleAsync();
        (await db.DisciplinaryMeasures.Where(m => m.DisciplinaryCaseId == warning.Summary.Id).Select(m => m.AppliedByUserId).SingleAsync())
            .Should().Be(approverId);
    }

    [Fact]
    public async Task EmployeeFilter_ReturnsOnlyThatEmployeesRecords()
    {
        await ScenarioAsync();
        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");

        var page = (await legal.GetFromJsonAsync<Result<PagedResult<RecordSummaryDto>>>($"/api/v1/disciplinary-records?employeeId={_employee}"))!.Data!;
        page.Items.Should().HaveCount(2).And.OnlyContain(i => i.EmployeeId == _employee);
    }

    [Fact]
    public async Task Stats_GroupByStatusAndType_WithPortugueseLabelsAndPercentages()
    {
        await ScenarioAsync();
        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");

        var stats = (await legal.GetFromJsonAsync<Result<RecordStatsDto>>("/api/v1/disciplinary-records/stats"))!.Data!;
        stats.Total.Should().Be(3);
        stats.Employees.Should().Be(2);
        stats.SuspensionDays.Should().Be(2);
        stats.AverageDaysToApproval.Should().Be(0);
        stats.ByStatus.Should().BeEquivalentTo(new[]
        {
            new CountItemDto((int)RecordStatus.Approved, "Aprovada", 2, 66.7m),
            new CountItemDto((int)RecordStatus.PendingApproval, "Aguardando aprovação", 1, 33.3m),
        });
        stats.ByType.Select(t => t.Label).Should().BeEquivalentTo("Advertência escrita", "Suspensão", "Advertência verbal");

        var filtered = (await legal.GetFromJsonAsync<Result<RecordStatsDto>>($"/api/v1/disciplinary-records/stats?measureType={(int)DisciplinaryMeasureType.Suspension}"))!.Data!;
        filtered.Total.Should().Be(1);
    }

    [Fact]
    public async Task Export_HasPortugueseHeaders_AndLabels()
    {
        var (warning, _, _) = await ScenarioAsync();
        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");

        var response = await legal.GetAsync("/api/v1/disciplinary-records/export");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csv = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()).TrimStart('﻿');
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();

        lines[0].Should().StartWith("Número;Matrícula;Funcionário;Gerência;Setor;Medida;Motivo;Data da medida;Início da suspensão")
            .And.Contain("Situação;Origem;Registrada por;Registrada em;Aplicado por;Aprovada em;Dias até a aprovação");
        lines.Should().HaveCount(4);
        var line = lines.Single(l => l.Contains(warning.Summary.CaseNumber));
        line.Should().Contain("\"Advertência escrita\"").And.Contain("\"Aprovada\"").And.Contain("\"encarregado\"").And.Contain("\"SIGH\"");
        csv.Should().NotContainAny("WrittenWarning", "Approved", "PendingApproval");
    }

    [Fact]
    public async Task Overview_CountsThePeriod_ComparesWithThePreviousOne_AndListsRecentActivities()
    {
        var (warning, suspension, pending) = await ScenarioAsync();
        using var legal = await AccessTestHelpers.LoginAsync(Factory, "juridico@sigh.test");

        var overview = (await legal.GetFromJsonAsync<Result<RecordOverviewDto>>("/api/v1/disciplinary-records/overview?period=Last30Days"))!.Data!;
        overview.To.Should().Be(_today);
        overview.From.Should().Be(_today.AddDays(-29));
        overview.Measures.Should().Be(new OverviewKpiDto(3, 0));
        overview.PendingApproval.Should().Be(1);
        overview.Suspensions.Should().Be(new OverviewKpiDto(1, 0));
        overview.SuspensionDays.Should().Be(new OverviewKpiDto(2, 0));
        overview.Employees.Should().Be(new OverviewKpiDto(2, 0));
        overview.ByMonth.Should().HaveCountGreaterThanOrEqualTo(6);
        overview.ByMonth.Sum(m => m.Count).Should().Be(3);

        overview.RecentActivities.Should().Contain(a => a.Kind == RecordActivityKind.Approved && a.RecordId == warning.Summary.Id && a.ActorName == "encarregado");
        overview.RecentActivities.Should().Contain(a => a.Kind == RecordActivityKind.Registered && a.RecordId == pending.Summary.Id && a.ActorName == "auxiliar");
        overview.RecentActivities.Should().Contain(a => a.RecordId == suspension.Summary.Id && a.MeasureTypeLabel == "Suspensão");
        overview.RecentActivities.Select(a => a.At).Should().BeInDescendingOrder();
    }

    [Theory]
    [InlineData(OverviewPeriod.Last30Days, "2026-11-15", "2026-10-17", "2026-09-17", "2026-10-16")]
    [InlineData(OverviewPeriod.Last90Days, "2026-11-15", "2026-08-18", "2026-05-20", "2026-08-17")]
    [InlineData(OverviewPeriod.Last12Months, "2026-11-15", "2025-12-01", "2024-12-01", "2025-11-30")]
    [InlineData(OverviewPeriod.CurrentYear, "2026-11-15", "2026-01-01", "2025-01-01", "2025-11-15")]
    public void PeriodRanges_HaveAPreviousPeriodOfTheSameSize(OverviewPeriod period, string today, string from, string previousFrom, string previousTo)
    {
        var (f, t, pf, pt) = RecordQueries.RangeOf(period, DateOnly.Parse(today));
        f.Should().Be(DateOnly.Parse(from));
        t.Should().Be(DateOnly.Parse(today));
        pf.Should().Be(DateOnly.Parse(previousFrom));
        pt.Should().Be(DateOnly.Parse(previousTo));
    }
}
