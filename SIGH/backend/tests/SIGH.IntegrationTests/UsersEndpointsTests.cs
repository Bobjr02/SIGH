using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Common.Models;
using SIGH.Application.Users.ChangeStatus;
using SIGH.Application.Users.CreateUser;
using SIGH.Application.Users.UnlockUser;
using SIGH.Domain.Enums;
using Xunit;

namespace SIGH.IntegrationTests;

public class UsersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task CreateUser_WithValidData_Returns201Created()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateUserRequest(
            FullName: "Novo Usuario Teste",
            Email: $"novo.usuario.{Guid.NewGuid():N}@sigh.com",
            Cpf: "11122233344",
            InitialPassword: "User@123456",
            MustChangePassword: true,
            RoleIds: new List<Guid>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<Result<CreateUserResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UnlockUser_LockedUser_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        // Obtém a lista de usuários para encontrar o ID do usuário bloqueado
        var usersResponse = await _client.GetAsync("/api/v1/users");
        var usersResult = await usersResponse.Content.ReadFromJsonAsync<Result<SIGH.Application.Users.GetUsers.PagedList<SIGH.Application.Users.GetUsers.UserSummaryDto>>>();
        var lockedUser = usersResult!.Data!.Items.FirstOrDefault(u => u.Email == "locked@sigh.com");

        lockedUser.Should().NotBeNull();

        var unlockRequest = new UnlockUserRequest(Reason: "Desbloqueio efetuado para testes de integração.");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{lockedUser!.Id}/unlock", unlockRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeStatus_ActiveUser_Returns200OK()
    {
        // Arrange
        await AuthenticateAsync();

        var usersResponse = await _client.GetAsync("/api/v1/users");
        var usersResult = await usersResponse.Content.ReadFromJsonAsync<Result<SIGH.Application.Users.GetUsers.PagedList<SIGH.Application.Users.GetUsers.UserSummaryDto>>>();
        var adminUser = usersResult!.Data!.Items.FirstOrDefault(u => u.Email == "admin@sigh.com");

        adminUser.Should().NotBeNull();

        var statusRequest = new ChangeUserStatusRequest(
            NewStatus: UserStatus.Inactive,
            Reason: "Inativação solicitada para teste de integração."
        );

        // Act
        var response = await _client.PatchAsync($"/api/v1/users/{adminUser!.Id}/status", JsonContent.Create(statusRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }
}
