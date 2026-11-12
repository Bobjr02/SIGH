using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Api.Contracts.Auth;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Api.Contracts.Disciplinary;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

public class DisciplinaryCasesEndpointsTests : IntegrationTestBase
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DisciplinaryCasesEndpointsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private const string AdminEmail = "admin@sigh.com";
    private const string ApproverEmail = "approver@sigh.com";
    private const string DefaultPassword = "Admin@123456";

    private async Task AuthenticateAsync()
    {
        var (token, _) = await LoginAsync(AdminEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<(string AccessToken, Guid UserId)> LoginAsync(string email)
    {
        if (_factory.TokenCache.TryGetValue(email, out var cached))
        {
            return cached;
        }

        var loginRequest = new LoginRequest(email, DefaultPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginHttpResponse>>();
        var entry = (loginResult!.Data!.AccessToken, loginResult.Data.User.Id);
        _factory.TokenCache[email] = entry;
        return entry;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(string email)
    {
        var (token, userId) = await LoginAsync(email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId);
    }

    /// <summary>
    /// Prepara um processo até a etapa "decisão registrada" usando o cliente informado como decisor.
    /// </summary>
    private async Task<(Guid CaseId, Guid DecisionId, Guid EmployeeId)> ArrangeCaseWithRecordedDecisionAsync(HttpClient deciderClient)
    {
        // Fixture local: Company + ManagementUnit + JobTitle + Employee reais, persistidos
        // no mesmo SQL Server da factory, para uso na etapa Add Employee.
        Employee employee;
        Company company;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SighDbContext>();

            company = Company.Create("Empresa Fluxo de Vida");
            var managementUnit = ManagementUnit.Create(company.Id, "Unidade Fluxo de Vida");
            var jobTitle = JobTitle.Create(company.Id, "Cargo Fluxo de Vida");
            employee = Employee.Create(
                companyId: company.Id,
                employeeNumber: $"EMP-{Guid.NewGuid():N}"[..12],
                fullName: "Funcionário Fluxo de Vida",
                cpf: "52998224725",
                admissionDate: DateOnly.FromDateTime(DateTime.UtcNow),
                jobTitleId: jobTitle.Id,
                managementUnitId: managementUnit.Id,
                initialStatus: EmployeeStatus.Active);

            context.Companies.Add(company);
            context.ManagementUnits.Add(managementUnit);
            context.JobTitles.Add(jobTitle);
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
        }

        var infTypeRequest = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10],
            Name: "Falta não justificada",
            DefaultSeverity: InfractionSeverity.Moderate);
        var infTypeResp = await deciderClient.PostAsJsonAsync("/api/v1/infraction-types", infTypeRequest);
        var infTypeResult = await infTypeResp.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        var infTypeId = infTypeResult!.Data!.Id;

        var createRequest = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: company.Id,
            Title: "Fluxo de vida do processo",
            Description: "Descrição do fluxo de vida do processo.");
        var createResp = await deciderClient.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        var createResult = await createResp.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        var caseId = createResult!.Data!.Id;

        // IDs de ator forjados no corpo devem ser ignorados (o ator vem do token).
        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/open", new { OpenedByUserId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var occurrenceRequest = new AddOccurrenceHttpRequest(DateTimeOffset.UtcNow, "Ocorrência registrada", infTypeId, InfractionSeverity.Moderate);
        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/occurrences", occurrenceRequest))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var empRequest = new AddEmployeeToCaseRequest(caseId, employee.Id, CaseEmployeeRole.Accused, true);
        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/employees", empRequest))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var evRequest = new AddEvidenceHttpRequest(
            EvidenceType.Document,
            "Folha de ponto",
            DateTimeOffset.UtcNow);
        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/evidences", evRequest))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/start-investigation", new { }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/submit-for-decision", new { }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Corpo com DecidedByUserId forjado: deve ser ignorado pela API.
        var decRequest = new
        {
            DecisionType = DecisionType.FormalWarning,
            Summary = "Advertência por escrito.",
            Reasoning = "A apuração confirmou a conduta que fundamenta a advertência.",
            DecidedByUserId = Guid.NewGuid()
        };
        var decResp = await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/decisions", decRequest);
        decResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var decResult = await decResp.Content.ReadFromJsonAsync<Result<RecordDecisionResponse>>();

        return (caseId, decResult!.Data!.DecisionId, employee.Id);
    }

    private async Task<DecisionDto> GetSingleDecisionAsync(HttpClient client, Guid caseId)
    {
        var getResp = await client.GetAsync($"/api/v1/disciplinary-cases/{caseId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResp.Content.ReadFromJsonAsync<Result<GetDisciplinaryCaseByIdResponse>>();
        return getResult!.Data!.Decisions.Should().ContainSingle().Subject;
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WithValidData_Returns201CreatedAndLocationHeader()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Insubordinação grave no setor de TI",
            Description: "Funcionário recusou-se a executar diretrizes de segurança.",
            Priority: DisciplinaryCasePriority.High);

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

        var request1 = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: caseNumber,
            CompanyId: companyId,
            Title: "Processo Original",
            Description: "Descrição do processo original.");
        await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", request1);

        var request2 = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: caseNumber,
            CompanyId: companyId,
            Title: "Processo Duplicado",
            Description: "Descrição do processo duplicado.");

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

        var request = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: "",
            CompanyId: Guid.Empty,
            Title: "",
            Description: "");

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

        var createRequest = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Consulta por ID",
            Description: "Descrição do processo para consulta.");

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
        // Arrange: admin registra a decisão; outro usuário (aprovador) aprova.
        var (deciderClient, deciderUserId) = await CreateAuthenticatedClientAsync(AdminEmail);
        var (approverClient, approverUserId) = await CreateAuthenticatedClientAsync(ApproverEmail);
        var (caseId, decisionId, employeeId) = await ArrangeCaseWithRecordedDecisionAsync(deciderClient);

        // Approve Decision (aprovador distinto do decisor)
        var appResp = await approverClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/approve-decision", new { });
        appResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Apply Measure
        var measureRequest = new ApplyMeasureHttpRequest(decisionId, employeeId, DisciplinaryMeasureType.WrittenWarning, "Advertência por escrito dada.", DateTimeOffset.UtcNow);
        var measureResp = await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/measures", measureRequest);
        measureResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Conclude Case
        var concludeRequest = new ConcludeDisciplinaryCaseRequest(caseId, "Resumo conclusivo: advertência aplicada.");
        var concludeResp = await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/conclude", concludeRequest);
        concludeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: atores gravados vêm dos tokens
        var getResp = await deciderClient.GetAsync($"/api/v1/disciplinary-cases/{caseId}");
        var details = (await getResp.Content.ReadFromJsonAsync<Result<GetDisciplinaryCaseByIdResponse>>())!.Data!;
        details.OpenedByUserId.Should().Be(deciderUserId);
        details.Decisions.Should().ContainSingle().Which.DecidedByUserId.Should().Be(deciderUserId);
        details.Decisions.Single().ApprovedByUserId.Should().Be(approverUserId);
    }

    [Fact]
    public async Task ApproveDecision_WhenBodyContainsAnotherUserId_IgnoresBodyAndPersistsTokenUser()
    {
        // Arrange
        var (deciderClient, deciderUserId) = await CreateAuthenticatedClientAsync(AdminEmail);
        var (approverClient, approverUserId) = await CreateAuthenticatedClientAsync(ApproverEmail);
        var (caseId, _, _) = await ArrangeCaseWithRecordedDecisionAsync(deciderClient);
        var spoofedUserId = Guid.NewGuid();

        // Act: tenta registrar a aprovação em nome de outra pessoa via corpo da requisição
        var response = await approverClient.PostAsJsonAsync(
            $"/api/v1/disciplinary-cases/{caseId}/approve-decision",
            new { DisciplinaryCaseId = caseId, ApprovedByUserId = spoofedUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decision = await GetSingleDecisionAsync(approverClient, caseId);
        decision.Status.Should().Be(DecisionStatus.Approved);
        decision.ApprovedByUserId.Should().Be(approverUserId);
        decision.ApprovedByUserId.Should().NotBe(spoofedUserId);
        decision.DecidedByUserId.Should().Be(deciderUserId);
    }

    [Fact]
    public async Task ApproveDecision_WhenSameUserRecordedDecision_Returns400BadRequest()
    {
        // Arrange
        var (deciderClient, _) = await CreateAuthenticatedClientAsync(AdminEmail);
        var (caseId, _, _) = await ArrangeCaseWithRecordedDecisionAsync(deciderClient);

        // Act: o próprio decisor tenta aprovar
        var response = await deciderClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/approve-decision", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<Result<object>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("não pode aprovar a própria decisão");

        var decision = await GetSingleDecisionAsync(deciderClient, caseId);
        decision.Status.Should().NotBe(DecisionStatus.Approved);
        decision.ApprovedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task RejectDecision_WhenSameUserRecordedDecision_Returns400BadRequest()
    {
        // Arrange
        var (deciderClient, _) = await CreateAuthenticatedClientAsync(AdminEmail);
        var (caseId, _, _) = await ArrangeCaseWithRecordedDecisionAsync(deciderClient);

        // Act
        var response = await deciderClient.PostAsJsonAsync(
            $"/api/v1/disciplinary-cases/{caseId}/reject-decision",
            new RejectDecisionHttpRequest("Rejeição pelo próprio decisor."));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DisciplinaryCase_Cancel_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: $"PROC-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Processo para Cancelamento",
            Description: "Descrição do processo para cancelamento.");
        var createResp = await _client.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        var createResult = await createResp.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>();
        var caseId = createResult!.Data!.Id;

        var cancelRequest = new CancelDisciplinaryCaseRequest(caseId, "Cancelado por inconsistência nos fatos.");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DisciplinaryCaseEndpoints_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var anonymousClient = Factory.CreateClient();

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
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginHttpResponse>>();

        var unprivilegedClient = Factory.CreateClient();
        unprivilegedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        // Act 1: Get Cases
        var getResponse = await unprivilegedClient.GetAsync("/api/v1/disciplinary-cases");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 2: Create Case
        var createRequest = new CreateDisciplinaryCaseHttpRequest(
            $"PROC-{Guid.NewGuid():N}"[..12],
            Guid.NewGuid(),
            "Sem permissão",
            "Descrição do processo sem permissão.");
        var createResponse = await unprivilegedClient.PostAsJsonAsync("/api/v1/disciplinary-cases", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 3: Approve Decision
        var approveResponse = await unprivilegedClient.PostAsJsonAsync($"/api/v1/disciplinary-cases/{Guid.NewGuid()}/approve-decision", new { });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
