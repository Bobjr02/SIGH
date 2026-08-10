using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.IntegrationTests;

public class InfractionTypesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InfractionTypesEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task CreateInfractionType_WithValidData_Returns201CreatedAndLocationHeader()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            Name: "Desídia nas funções",
            DefaultSeverity: InfractionSeverity.Moderate,
            Description: "Atrasos e faltas sistemáticas sem justificativa.",
            LegalReference: "Artigo 482 CLT");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/infraction-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        result.Data.Code.Should().Be(request.Code);
    }

    [Fact]
    public async Task CreateInfractionType_WhenDuplicateCode_Returns409Conflict()
    {
        // Arrange
        await AuthenticateAsync();

        var code = $"INF-{Guid.NewGuid():N}"[..10];
        var request1 = new CreateInfractionTypeRequest(Code: code, Name: "Infração Original", DefaultSeverity: InfractionSeverity.Low);
        await _client.PostAsJsonAsync("/api/v1/infraction-types", request1);

        var request2 = new CreateInfractionTypeRequest(Code: code, Name: "Infração Duplicada", DefaultSeverity: InfractionSeverity.High);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/infraction-types", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateInfractionType_WhenInvalidRequest_Returns400BadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateInfractionTypeRequest(Code: "", Name: "", DefaultSeverity: InfractionSeverity.Low);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/infraction-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetInfractionTypeById_WhenExists_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10],
            Name: "Insubordinação",
            DefaultSeverity: InfractionSeverity.High);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/infraction-types", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        var createdId = createResult!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/infraction-types/{createdId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InfractionTypeDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(createdId);
        result.Data.Name.Should().Be("Insubordinação");
    }

    [Fact]
    public async Task GetInfractionTypeById_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        await AuthenticateAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/infraction-types/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInfractionTypes_Returns200OKWithPagedResultContract()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/infraction-types?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<InfractionTypeDto>>>();
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
    public async Task UpdateInfractionType_WhenValid_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10],
            Name: "Nome Antigo",
            DefaultSeverity: InfractionSeverity.Low);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/infraction-types", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        var createdId = createResult!.Data!.Id;

        var updateRequest = new UpdateInfractionTypeRequest(
            Id: createdId,
            Name: "Nome Atualizado",
            DefaultSeverity: InfractionSeverity.High,
            RequiresFormalInvestigation: true,
            AllowsTerminationRecommendation: false,
            Description: "Descrição atualizada",
            LegalReference: "CLT Art. 482 h");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/infraction-types/{createdId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<UpdateInfractionTypeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task ActivateAndDeactivate_WhenValid_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new CreateInfractionTypeRequest(
            Code: $"INF-{Guid.NewGuid():N}"[..10],
            Name: "Infração para ativar/desativar",
            DefaultSeverity: InfractionSeverity.Low);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/infraction-types", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>();
        var createdId = createResult!.Data!.Id;

        // Act 1: Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/infraction-types/{createdId}/deactivate", null);

        // Assert 1
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivateResult = await deactivateResponse.Content.ReadFromJsonAsync<Result<DeactivateInfractionTypeResponse>>();
        deactivateResult!.Data!.IsActive.Should().BeFalse();

        // Act 2: Activate
        var activateResponse = await _client.PostAsync($"/api/v1/infraction-types/{createdId}/activate", null);

        // Assert 2
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activateResult = await activateResponse.Content.ReadFromJsonAsync<Result<ActivateInfractionTypeResponse>>();
        activateResult!.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Endpoint_WhenUnauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var factory = new CustomWebApplicationFactory();
        var anonymousClient = factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("/api/v1/infraction-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoint_WhenUserLacksPermissions_Returns403Forbidden()
    {
        // Arrange
        var loginRequest = new LoginRequest("unprivileged@sigh.com", "Admin@123456");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>();

        var unprivilegedClient = new CustomWebApplicationFactory().CreateClient();
        unprivilegedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        // Act
        var response = await unprivilegedClient.GetAsync("/api/v1/infraction-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
