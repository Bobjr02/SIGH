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
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Parte A: o fluxo real do RH (o auxiliar registra a medida já decidida; o encarregado aprova ou rejeita),
/// executado pela fachada sobre o domínio existente.
/// </summary>
public class DisciplinaryRecordsTests : IntegrationTestBase
{
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    public DisciplinaryRecordsTests(CustomWebApplicationFactory factory) : base(factory) { }

    private Guid _employee;
    private Guid _infraction;
    private DateOnly _today;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ReferenceData.LoadEmployeesAsync(Factory);
        _employee = await ReferenceData.EmployeeIdAsync(Factory, "50090006");
        _infraction = await ReferenceData.InfractionTypeAsync(Factory);
        _today = BusinessCalendar.Today(Factory.Clock.UtcNow);
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        await AccessTestHelpers.CreateUserAsync(Factory, "encarregado@sigh.test", SystemRoles.HrApprover);
    }

    private Task<HttpClient> Registrar() => AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
    private Task<HttpClient> Approver() => AccessTestHelpers.LoginAsync(Factory, "encarregado@sigh.test");

    private static async Task<string> DetailAsync(HttpResponseMessage response)
    {
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return root.TryGetProperty("detail", out var d) ? d.GetString()! : root.GetProperty("message").GetString()!;
    }

    [Fact]
    public async Task Register_RunsTheRealDomainSteps_AndLeavesTheRecordAwaitingApproval()
    {
        using var registrar = await Registrar();
        var record = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-2)));

        record.Summary.Status.Should().Be(RecordStatus.PendingApproval);
        record.Summary.StatusLabel.Should().Be("Aguardando aprovação");
        record.Summary.EmployeeNumber.Should().Be("50090006");
        record.Summary.ManagementUnitCode.Should().Be("USGA131");
        record.ManagementUnitCodeAtTime.Should().Be("USGA131");

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var entity = await db.DisciplinaryCases.Include(c => c.Decisions).Include(c => c.Employees).Include(c => c.Occurrences)
            .SingleAsync(c => c.Id == record.Summary.Id);
        entity.Kind.Should().Be(DisciplinaryCaseKind.Record);
        entity.Status.Should().Be(DisciplinaryCaseStatus.AwaitingDecision);
        entity.Title.Should().Be("Medida – Advertência escrita – 50090006");
        var registrarId = await db.Users.Where(u => u.Email == "auxiliar@sigh.test").Select(u => u.Id).SingleAsync();
        entity.OpenedByUserId.Should().Be(registrarId, "o registrante vem do token");
        entity.Decisions.Should().ContainSingle(d => d.Status == DecisionStatus.PendingApproval && d.DecidedByUserId == registrarId
                                                     && d.ProposedMeasureType == DisciplinaryMeasureType.WrittenWarning);
        entity.Employees.Should().ContainSingle(e => e.Role == CaseEmployeeRole.Accused && e.EmployeeId == _employee && e.ManagementUnitIdAtTime != null);
        entity.Occurrences.Should().ContainSingle(o => o.InfractionTypeId == _infraction);

        // Avisa os aprovadores (no sistema e por e-mail), nunca o próprio registrante.
        var approverId = await db.Users.Where(u => u.Email == "encarregado@sigh.test").Select(u => u.Id).SingleAsync();
        (await db.Notifications.CountAsync(n => n.UserId == approverId && n.DisciplinaryCaseId == entity.Id)).Should().Be(1);
        (await db.Notifications.AnyAsync(n => n.UserId == registrarId)).Should().BeFalse();
        (await db.EmailOutbox.AnyAsync(m => m.To == "encarregado@sigh.test")).Should().BeTrue();
    }

    [Fact]
    public async Task Approve_AppliesTheMeasureAndConcludes_AndNotifiesTheRegistrant()
    {
        using var registrar = await Registrar();
        var record = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today));

        using var approver = await Approver();
        (await approver.GetFromJsonAsync<Result<int>>("/api/v1/disciplinary-records/pending-count"))!.Data.Should().Be(1);
        var detail = (await approver.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{record.Summary.Id}"))!.Data!;
        detail.Actions.CanApprove.Should().BeTrue();

        var approved = await approver.PostAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/approve", null);
        approved.StatusCode.Should().Be(HttpStatusCode.OK, await approved.Content.ReadAsStringAsync());
        var body = (await approved.Content.ReadFromJsonAsync<Result<RecordDetailDto>>())!.Data!;
        body.Summary.Status.Should().Be(RecordStatus.Approved);
        body.ApprovedByName.Should().Be("encarregado");

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var entity = await db.DisciplinaryCases.Include(c => c.Measures).SingleAsync(c => c.Id == record.Summary.Id);
        entity.Status.Should().Be(DisciplinaryCaseStatus.Completed);
        entity.Measures.Should().ContainSingle(m => m.Status == DisciplinaryMeasureStatus.Applied && m.MeasureType == DisciplinaryMeasureType.WrittenWarning);
        var registrarId = await db.Users.Where(u => u.Email == "auxiliar@sigh.test").Select(u => u.Id).SingleAsync();
        (await db.Notifications.AnyAsync(n => n.UserId == registrarId && n.Type == RecordNotifier.ApprovedType)).Should().BeTrue();
    }

    [Fact]
    public async Task JustCauseTermination_DoesNotChangeTheEmployeeStatus_SapIsTheSourceOfTruth()
    {
        using var registrar = await Registrar();
        var input = ReferenceData.Warning(_employee, _infraction, _today) with { MeasureType = DisciplinaryMeasureType.JustCauseTermination };
        var record = await ReferenceData.RegisterAsync(registrar, input);
        using var approver = await Approver();
        (await approver.PostAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/approve", null)).EnsureSuccessStatusCode();

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.Employees.SingleAsync(e => e.Id == _employee)).Status.Should().Be(EmployeeStatus.Active);
        (await db.DisciplinaryMeasures.SingleAsync(m => m.DisciplinaryCaseId == record.Summary.Id)).MeasureType
            .Should().Be(DisciplinaryMeasureType.JustCauseTermination);
    }

    [Fact]
    public async Task WhoRegistersNeverApproves_EvenWithBothProfiles_AndTheAdministratorCannotApproveAtAll()
    {
        await AccessTestHelpers.CreateUserAsync(Factory, "duplo@sigh.test", [new RoleGrant(SystemRoles.HrRegistrar), new RoleGrant(SystemRoles.HrApprover)]);
        await AccessTestHelpers.CreateUserAsync(Factory, "admin.real@sigh.test", SystemRoles.Administrator);
        using var both = await AccessTestHelpers.LoginAsync(Factory, "duplo@sigh.test");
        var record = await ReferenceData.RegisterAsync(both, ReferenceData.Warning(_employee, _infraction, _today));

        var detail = (await both.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{record.Summary.Id}"))!.Data!;
        detail.Actions.CanApprove.Should().BeFalse("quem registra nunca aprova a mesma medida");
        (await both.GetFromJsonAsync<Result<int>>("/api/v1/disciplinary-records/pending-count"))!.Data.Should().Be(0);
        (await both.PostAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await both.PostAsJsonAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/reject", new ReasonInput("x"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var admin = await AccessTestHelpers.LoginAsync(Factory, "admin.real@sigh.test");
        (await admin.PostAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PostAsJsonAsync("/api/v1/disciplinary-records", ReferenceData.Warning(_employee, _infraction, _today.AddDays(-1))))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "o Administrador não registra medidas");
        var adminView = (await admin.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{record.Summary.Id}"))!.Data!;
        adminView.Actions.CanApprove.Should().BeFalse();
    }

    [Fact]
    public async Task Reject_RequiresAReason_ThenTheRegistrantCorrects_AndTheApproverApproves()
    {
        using var registrar = await Registrar();
        var record = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-3)));
        var id = record.Summary.Id;
        using var approver = await Approver();

        (await approver.PostAsJsonAsync($"/api/v1/disciplinary-records/{id}/reject", new ReasonInput("  "))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var rejected = await approver.PostAsJsonAsync($"/api/v1/disciplinary-records/{id}/reject", new ReasonInput("Data da ocorrência errada."));
        rejected.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejectedBody = (await rejected.Content.ReadFromJsonAsync<Result<RecordDetailDto>>())!.Data!;
        rejectedBody.Summary.Status.Should().Be(RecordStatus.Rejected);
        rejectedBody.LastRejectionReason.Should().Be("Data da ocorrência errada.");

        // Rejeitada não pode ser aprovada; o registrante vê os botões de corrigir e cancelar.
        (await approver.PostAsync($"/api/v1/disciplinary-records/{id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var forRegistrar = (await registrar.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{id}"))!.Data!;
        forRegistrar.Actions.CanCorrect.Should().BeTrue();
        forRegistrar.Actions.CanCancel.Should().BeTrue();

        var corrected = await registrar.PutAsJsonAsync($"/api/v1/disciplinary-records/{id}",
            ReferenceData.Warning(_employee, _infraction, _today.AddDays(-1), "Faltou ao turno; data corrigida."));
        corrected.StatusCode.Should().Be(HttpStatusCode.OK, await corrected.Content.ReadAsStringAsync());
        var correctedBody = (await corrected.Content.ReadFromJsonAsync<Result<RecordSaveResult>>())!.Data!.Record!;
        correctedBody.Summary.Status.Should().Be(RecordStatus.PendingApproval);
        correctedBody.OccurrenceDates.Should().Equal(_today.AddDays(-1));
        correctedBody.History.Should().HaveCount(2);

        (await approver.PostAsync($"/api/v1/disciplinary-records/{id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ARejectedRecord_CanBeCorrectedByAnotherRegistrar_OrCancelledWithAReason()
    {
        using var registrar = await Registrar();
        var first = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-5)));
        var second = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-4)));
        using var approver = await Approver();
        foreach (var id in new[] { first.Summary.Id, second.Summary.Id })
            (await approver.PostAsJsonAsync($"/api/v1/disciplinary-records/{id}/reject", new ReasonInput("Rever."))).EnsureSuccessStatusCode();

        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar2@sigh.test", SystemRoles.HrRegistrar);
        using var other = await AccessTestHelpers.LoginAsync(Factory, "auxiliar2@sigh.test");
        (await other.PutAsJsonAsync($"/api/v1/disciplinary-records/{first.Summary.Id}", ReferenceData.Warning(_employee, _infraction, _today.AddDays(-5))))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await registrar.PostAsJsonAsync($"/api/v1/disciplinary-records/{second.Summary.Id}/cancel", new ReasonInput(""))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var cancelled = await registrar.PostAsJsonAsync($"/api/v1/disciplinary-records/{second.Summary.Id}/cancel", new ReasonInput("Registrada em duplicidade."));
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancelled.Content.ReadFromJsonAsync<Result<RecordDetailDto>>())!.Data!.Summary.Status.Should().Be(RecordStatus.Cancelled);

        // Pendente não se cancela (só rejeitada).
        (await registrar.PostAsJsonAsync($"/api/v1/disciplinary-records/{first.Summary.Id}/cancel", new ReasonInput("x"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Attachments_WhilePendingAndAfterApproval_NeverAfterCancellation()
    {
        using var registrar = await Registrar();
        using var approver = await Approver();
        var record = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today));
        var id = record.Summary.Id;

        (await AttachAsync(registrar, id, "Carta assinada")).Should().Be(HttpStatusCode.OK, "pendente aceita anexo");
        (await approver.PostAsync($"/api/v1/disciplinary-records/{id}/approve", null)).EnsureSuccessStatusCode();
        (await AttachAsync(registrar, id, "Carta assinada (foto)")).Should().Be(HttpStatusCode.OK, "aprovada (concluída) ainda aceita a carta assinada");

        var detail = (await registrar.GetFromJsonAsync<Result<RecordDetailDto>>($"/api/v1/disciplinary-records/{id}"))!.Data!;
        detail.Evidences.Should().HaveCount(2).And.OnlyContain(e => e.HasFile);

        var other = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-6)));
        (await approver.PostAsJsonAsync($"/api/v1/disciplinary-records/{other.Summary.Id}/reject", new ReasonInput("Não procede."))).EnsureSuccessStatusCode();
        (await registrar.PostAsJsonAsync($"/api/v1/disciplinary-records/{other.Summary.Id}/cancel", new ReasonInput("Não procede."))).EnsureSuccessStatusCode();
        (await AttachAsync(registrar, other.Summary.Id, "Tarde demais")).Should().Be(HttpStatusCode.BadRequest, "cancelada nunca recebe anexo");
    }

    private static async Task<HttpStatusCode> AttachAsync(HttpClient client, Guid recordId, string title)
    {
        var created = await client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{recordId}/evidences",
            new AddEvidenceHttpRequest(EvidenceType.Document, title, DateTimeOffset.UtcNow));
        if (!created.IsSuccessStatusCode)
            return created.StatusCode;
        var evidenceId = (await created.Content.ReadFromJsonAsync<Result<AddEvidenceResponse>>())!.Data!.Id;

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(PdfBytes), "file", "carta.pdf");
        return (await client.PostAsync($"/api/v1/disciplinary-cases/{recordId}/evidences/{evidenceId}/file", form)).StatusCode;
    }

    [Fact]
    public async Task Validation_TerminatedEmployee_FutureOrMissingOccurrence_AndSuspensionPeriod()
    {
        using var registrar = await Registrar();
        var basic = ReferenceData.Warning(_employee, _infraction, _today);

        async Task<string> Rejects(RecordInput input)
        {
            var response = await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", input);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
            return await DetailAsync(response);
        }

        (await Rejects(basic with { OccurrenceDates = [] })).Should().Contain("pelo menos uma data");
        (await Rejects(basic with { OccurrenceDates = [_today.AddDays(1)], MeasureDate = _today.AddDays(1) })).Should().Contain("futuro");
        (await Rejects(basic with { Description = " " })).Should().Contain("descrição");
        (await Rejects(basic with { MeasureType = DisciplinaryMeasureType.Suspension })).Should().Contain("início e de fim");
        (await Rejects(basic with { MeasureType = DisciplinaryMeasureType.TerminationRecommendation })).Should().Contain("não aceito");
        (await Rejects(basic with { MeasureType = DisciplinaryMeasureType.Guidance })).Should().Contain("não aceito");

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var employee = await db.Employees.SingleAsync(e => e.EmployeeNumber == "50090012");
            employee.Terminate(DateOnly.FromDateTime(DateTime.Today), "Desligado no SAP");
            await db.SaveChangesAsync();
        }
        var terminated = await ReferenceData.EmployeeIdAsync(Factory, "50090012");
        (await Rejects(basic with { EmployeeId = terminated })).Should().Contain("desligado");
    }

    [Fact]
    public async Task Suspension_CountsBothEnds_AndAnOverlappingPendingOrApprovedSuspensionIsBlocked()
    {
        using var registrar = await Registrar();
        var suspension = ReferenceData.Warning(_employee, _infraction, _today.AddDays(-1)) with
        {
            MeasureType = DisciplinaryMeasureType.Suspension,
            MeasureDate = _today,
            SuspensionStart = _today.AddDays(2),
            SuspensionEnd = _today.AddDays(4),
        };
        var record = await ReferenceData.RegisterAsync(registrar, suspension);
        record.Summary.SuspensionDays.Should().Be(3, "conta o primeiro e o último dia");

        var overlapping = suspension with { OccurrenceDates = [_today.AddDays(-2)], SuspensionStart = _today.AddDays(4), SuspensionEnd = _today.AddDays(6) };
        var response = await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", overlapping);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailAsync(response)).Should().Contain("sobrepõe").And.Contain(record.Summary.CaseNumber);

        // Depois de rejeitada (e cancelada), a suspensão não bloqueia mais.
        using var approver = await Approver();
        (await approver.PostAsJsonAsync($"/api/v1/disciplinary-records/{record.Summary.Id}/reject", new ReasonInput("Período errado."))).EnsureSuccessStatusCode();
        (await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", overlapping)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PossibleDuplicate_IsAWarningThatRequiresConfirmation()
    {
        using var registrar = await Registrar();
        var input = ReferenceData.Warning(_employee, _infraction, _today.AddDays(-1));
        var first = await ReferenceData.RegisterAsync(registrar, input);

        var warning = await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", input);
        warning.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = (await warning.Content.ReadFromJsonAsync<Result<RecordSaveResult>>())!;
        body.ErrorCode.Should().Be("POSSIBLE_DUPLICATE");
        body.Data!.PossibleDuplicates.Should().ContainSingle(d => d.Id == first.Summary.Id && d.OccurrenceDate == _today.AddDays(-1));

        // Outro tipo de medida na mesma data não é duplicidade.
        (await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", input with { MeasureType = DisciplinaryMeasureType.VerbalWarning }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var confirmed = await registrar.PostAsJsonAsync("/api/v1/disciplinary-records", input with { ConfirmPossibleDuplicate = true });
        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task List_FiltersByStatusTypeReasonUnitPeriodAndEmployee_AndPaginates()
    {
        using var registrar = await Registrar();
        var e132 = await ReferenceData.EmployeeIdAsync(Factory, "50090000");
        var a = await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today.AddDays(-10)));
        await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(e132, _infraction, _today.AddDays(-2)) with { MeasureType = DisciplinaryMeasureType.VerbalWarning });
        using var approver = await Approver();
        (await approver.PostAsync($"/api/v1/disciplinary-records/{a.Summary.Id}/approve", null)).EnsureSuccessStatusCode();

        async Task<PagedResult<RecordSummaryDto>> List(string query) =>
            (await registrar.GetFromJsonAsync<Result<PagedResult<RecordSummaryDto>>>($"/api/v1/disciplinary-records?{query}"))!.Data!;

        (await List("")).TotalCount.Should().Be(2);
        (await List($"status={(int)RecordStatus.Approved}")).Items.Should().ContainSingle(r => r.EmployeeNumber == "50090006");
        (await List($"measureType={(int)DisciplinaryMeasureType.VerbalWarning}")).Items.Should().ContainSingle(r => r.EmployeeNumber == "50090000");
        (await List($"infractionTypeId={_infraction}")).TotalCount.Should().Be(2);
        var options = (await registrar.GetFromJsonAsync<Result<RecordOptionsDto>>("/api/v1/disciplinary-records/options"))!.Data!;
        var u131 = options.ManagementUnits.Single(m => m.Code == "USGA131").Id;
        (await List($"managementUnitId={u131}")).Items.Should().ContainSingle(r => r.EmployeeNumber == "50090006");
        (await List($"from={_today.AddDays(-3):yyyy-MM-dd}&to={_today:yyyy-MM-dd}")).Items.Should().ContainSingle(r => r.EmployeeNumber == "50090000");
        (await List("employee=50090006")).Items.Should().ContainSingle();
        (await List("employee=FRANCISCA MARIA")).Items.Should().ContainSingle(r => r.EmployeeNumber == "50090000");
        (await List("mine=true")).TotalCount.Should().Be(2);
        var page = await List("pageSize=1&pageNumber=2");
        page.Items.Should().HaveCount(1);
        page.TotalPages.Should().Be(2);
        options.MeasureTypes.Select(t => t.Label).Should().Equal("Advertência verbal", "Advertência escrita", "Suspensão", "Demissão por justa causa");
    }

    [Fact]
    public async Task PendingForMoreThanTheConfiguredDays_AlertsTheApproversOncePerDay()
    {
        using var registrar = await Registrar();
        await ReferenceData.RegisterAsync(registrar, ReferenceData.Warning(_employee, _infraction, _today));

        async Task<int> RunAsync()
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IRecordPendingAlertProcessor>().ProcessAsync();
        }

        // Aprovadores: o encarregado e os usuários da fixture (perfil de teste com todas as permissões).
        (await RunAsync()).Should().Be(0, "ainda não passou o prazo (3 dias)");
        Factory.Clock.Advance(TimeSpan.FromDays(3) + TimeSpan.FromHours(1));
        var approvers = await RunAsync();
        approvers.Should().BeGreaterThan(0);
        (await RunAsync()).Should().Be(0, "uma vez por dia");
        Factory.Clock.Advance(TimeSpan.FromDays(1));
        (await RunAsync()).Should().Be(approvers, "no dia seguinte avisa de novo");

        await using var check = Factory.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<SighDbContext>();
        var approverId = await db.Users.Where(u => u.Email == "encarregado@sigh.test").Select(u => u.Id).SingleAsync();
        (await db.Notifications.CountAsync(n => n.UserId == approverId && n.Type == RecordNotifier.PendingDigestType)).Should().Be(2);
    }
}
