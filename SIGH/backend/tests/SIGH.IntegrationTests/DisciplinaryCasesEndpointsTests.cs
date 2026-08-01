using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;
using SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.IntegrationTests;

public class DisciplinaryCasesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DisciplinaryCasesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var loginRequest = new LoginRequest("admin@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WithValidData_Returns201CreatedAndLocationHeader()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateDisciplinaryCaseRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Insubordinação grave no setor de TI",
            Description: "Funcionário recusou-se a executar diretrizes de segurança.",
            Priority: CasePriority.High);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        result.Data.CaseNumber.Should().Be(request.CaseNumber);
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WhenDuplicateCaseNumber_Returns409Conflict()
    {
        // Arrange
        await AuthenticateAsync();

        var caseNumber = $"PROC-{Guid.NewGuid():N}"[..12];
        var companyId = Guid.NewGuid();

        var request1 = new CreateDisciplinaryCaseRequest(CaseNumber: caseNumber, CompanyId: companyId, Title: "Processo Original");
        await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", request1);

        var request2 = new CreateDisciplinaryCaseRequest(CaseNumber: caseNumber, CompanyId: companyId, Title: "Processo Duplicado");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WhenInvalidRequest_Returns400BadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateDisciplinaryCaseRequest(CaseNumber: "", CompanyId: Guid.Empty, Title: "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDisciplinaryCaseById_WhenExists_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateDisciplinaryCaseRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Consulta por ID");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        var caseId = createResult!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/disciplinary-cases/{caseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<GetDisciplinaryCaseByIdResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(caseId);
    }

    [Fact]
    public async Task GetDisciplinaryCaseById_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/disciplinary-cases/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDisciplinaryCases_Returns200OKWithPagedResultContract()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/disciplinary-cases?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<DisciplinaryCaseSummaryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeNull();
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalCount.Should().BeGreaterOrEqualTo(0);
        result.Data.TotalPages.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task DisciplinaryCase_LifecycleFlow_ExecutesSuccessfully()
    {
        // Arrange
        await AuthenticateAsync();

        // 1. Create InfractionType
        var infTypeRequest = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10],
            Name: "Falta não justificada",
            DefaultSeverity: InfractionSeverity.Medium);
        var infTypeResp = await _client.PostAsJsonAsync("/api/v1/infraction-types", infTypeRequest);
        var infTypeResult = await infTypeResp.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        var infTypeId = infTypeResult!.Data!.Id;

        // 2. Create DisciplinaryCase
        var createRequest = new CreateDisciplinaryCaseRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Fluxo de vida do processo");
        var createResp = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        var createResult = await createResp.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        var caseId = createResult!.Data!.Id;

        // 3. Open Case
        var openResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/open", new OpenCaseRequest(caseId, Guid.NewGuid()));
        openResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Add Occurrence
        var occurrenceRequest = new AddOccurrenceRequest(caseId, DateTimeOffset.UtcNow, "Ocorrência registrada", Guid.NewGuid(), infTypeId, InfractionSeverity.Medium);
        var occResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/occurrences", occurrenceRequest);
        occResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Add Employee
        var empRequest = new AddEmployeeToCaseRequest(caseId, Guid.NewGuid(), CaseEmployeeRole.Accused, true);
        var empResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/employees", empRequest);
        empResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Add Evidence
        var evRequest = new AddEvidenceRequest(
            caseId,
            EvidenceType.Document,
            "Folha de ponto",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            StorageReference: "evidences/folha-ponto.pdf",
            OriginalFileName: "folha-ponto.pdf",
            ContentType: "application/pdf",
            FileSize: 1024);
        var evResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/evidences", evRequest);
        evResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Start Investigation
        var startInvResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/start-investigation", new StartInvestigationRequest(caseId, Guid.NewGuid()));
        startInvResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Submit For Decision
        var submitResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/submit-for-decision", new SubmitCaseForDecisionRequest(caseId, Guid.NewGuid()));
        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Record Decision
        var decRequest = new RecordDecisionRequest(
            caseId,
            DecisionType.FormalWarning,
            "Advertência por escrito.",
            "A apuração confirmou a conduta que fundamenta a advertência.",
            Guid.NewGuid());
        var decResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/decisions", decRequest);
        decResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var decResult = await decResp.Content.ReadFromJsonAsync<Result<RecordDecisionResponse>>();
        var decisionId = decResult!.Data!.DecisionId;

        // 10. Approve Decision
        var appResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/approve-decision", new ApproveDecisionRequest(caseId, Guid.NewGuid()));
        appResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 11. Apply Measure
        var measureRequest = new ApplyMeasureRequest(caseId, decisionId, empRequest.EmployeeId, DisciplinaryMeasureType.WrittenWarning, "Advertência por escrito dada.", DateTimeOffset.UtcNow, Guid.NewGuid());
        var measureResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/measures", measureRequest);
        measureResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 12. Conclude Case
        var concludeRequest = new ConcludeDisciplinaryCaseRequest(caseId, Guid.NewGuid(), "Resumo conclusivo: advertência aplicada.");
        var concludeResp = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/conclude", concludeRequest);
        concludeResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DisciplinaryCase_Cancel_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateDisciplinaryCaseRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Processo para Cancelamento");
        var createResp = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        var createResult = await createResp.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        var caseId = createResult!.Data!.Id;

        var cancelRequest = new CancelDisciplinaryCaseRequest(caseId, Guid.NewGuid(), "Cancelado por inconsistência nos fatos.");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DisciplinaryCaseEndpoints_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var anonymousClient = new CustomWebApplicationFactory().CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("/api/v1/disciplinary-cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisciplinaryCaseEndpoints_WhenUserLacksPermissions_Returns403Forbidden()
    {
        // Arrange
        var loginRequest = new LoginRequest("unprivileged@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        var unprivilegedClient = new CustomWebApplicationFactory().CreateClient();
        unprivilegedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        // Act 1: Get Cases
        var getResponse = await unprivilegedClient.GetAsync("/api/v1/disciplinary-cases");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 2: Create Case
        var createRequest = new CreateDisciplinaryCaseRequest($"PROC-{Guid.NewGuid():N}"[..12], Guid.NewGuid(), "Sem permissão");
        var createResponse = await unprivilegedClient.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 3: Approve Decision
        var approveResponse = await unprivilegedClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{Guid.NewGuid()}/approve-decision", new ApproveDecisionRequest(Guid.NewGuid(), Guid.NewGuid()));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
