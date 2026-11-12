using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// CreatedBy/UpdatedBy são preenchidos com o usuário do access token (CurrentUserService).
/// </summary>
public class AuditTrailTests : IntegrationTestBase
{
    public AuditTrailTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreatingThroughApi_RecordsCreatedByWithTokenUser()
    {
        using var admin = await CreateAuthenticatedClientAsync("admin@sigh.com");

        var id = await CreateInfractionTypeAsync(admin);

        var entity = await FindInfractionTypeAsync(id);
        entity.CreatedBy.Should().Be(CustomWebApplicationFactory.AdminUserId);
        entity.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task UpdatingThroughApi_RecordsUpdatedByWithTokenUser()
    {
        using var admin = await CreateAuthenticatedClientAsync("admin@sigh.com");
        using var approver = await CreateAuthenticatedClientAsync("approver@sigh.com");
        var id = await CreateInfractionTypeAsync(admin);

        // Outro usuário altera: UpdatedBy deve ser ele, e CreatedBy continua sendo o autor.
        var response = await approver.PutAsJsonAsync($"/api/v1/infraction-types/{id}", new UpdateInfractionTypeRequest(
            Id: id,
            Name: "Nome alterado na auditoria",
            DefaultSeverity: InfractionSeverity.High,
            RequiresFormalInvestigation: true,
            AllowsTerminationRecommendation: false));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entity = await FindInfractionTypeAsync(id);
        entity.CreatedBy.Should().Be(CustomWebApplicationFactory.AdminUserId);
        entity.UpdatedBy.Should().Be(CustomWebApplicationFactory.ApproverUserId);
        entity.UpdatedAt.Should().NotBeNull();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, email, CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateInfractionTypeAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/infraction-types", new CreateInfractionTypeRequest(
            Code: $"AUD-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            Name: "Tipo para auditoria",
            DefaultSeverity: InfractionSeverity.Moderate));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Result<CreateInfractionTypeResponse>>())!.Data!.Id;
    }

    private async Task<SIGH.Domain.Disciplinary.Entities.InfractionType> FindInfractionTypeAsync(Guid id)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        return await db.InfractionTypes.AsNoTracking().SingleAsync(i => i.Id == id);
    }
}
