using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Interfaces;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Host da API para os testes de integração, contra SQL Server real (Testcontainers).
/// Toda a configuração vem daqui: nenhum appsettings de desenvolvimento, user-secret ou segredo real.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Senha dos usuários de fixture (admin, approver, locked, unprivileged).</summary>
    public const string DefaultPassword = "Admin@123456";

    /// <summary>Administrador criado pelo DatabaseSeeder real: troca de senha obrigatória.</summary>
    public const string SeedAdminEmail = "seed.admin@sigh.test";
    public const string SeedAdminPassword = "Seed@Admin2026!";

    // IDs fixos: o reset entre testes recria os usuários com os mesmos IDs, então
    // access tokens guardados no TokenCache continuam válidos.
    public static readonly Guid AdminUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ApproverUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid LockedUserId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid UnprivilegedUserId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    // Chave de assinatura exclusiva dos testes (não é segredo; 32+ bytes para passar na validação).
    private const string TestJwtSecretKey = "integration-tests-only-signing-key-not-a-secret-0123456789";

    private string _connectionString = string.Empty;
    private string? _fixturePasswordHash;

    /// <summary>
    /// Cache de tokens por e-mail, compartilhado entre os testes da mesma fixture.
    /// Evita estourar o rate limit do endpoint de login (10 req/min por IP).
    /// </summary>
    public System.Collections.Concurrent.ConcurrentDictionary<string, (string AccessToken, Guid UserId)> TokenCache { get; } = new();

    /// <summary>Relógio da API (IDateTimeProvider). Volta ao tempo real a cada reset do banco.</summary>
    public TestClock Clock { get; } = new();

    public CustomWebApplicationFactory()
    {
        // HTTPS: o cookie do refresh token é Secure e só é reenviado pelo HttpClient em https.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public async Task InitializeAsync()
    {
        _connectionString = await SqlServerTestDatabase.GetConnectionStringAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing": não carrega appsettings.Development.json nem user-secrets da máquina.
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(Clock);
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, LoopbackRemoteIpStartupFilter>();
        });

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["JwtOptions:SecretKey"] = TestJwtSecretKey,
                ["JwtOptions:Issuer"] = "SIGH.Api.IntegrationTests",
                ["JwtOptions:Audience"] = "SIGH.Client.IntegrationTests",
                ["JwtOptions:ExpiryInMinutes"] = "60",
                ["Seed:AdminEmail"] = SeedAdminEmail,
                ["Seed:AdminPassword"] = SeedAdminPassword,
                ["Seed:DefaultCompanyName"] = "Empresa Padrão (Testes)",
                // O schema é criado uma vez por execução em SqlServerTestDatabase.
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["BackgroundWorkerOptions:Enabled"] = "false"
            });
        });
    }

    /// <summary>
    /// Limpa os dados (Respawn), roda o DatabaseSeeder real e recria os usuários de fixture.
    /// Chamado antes de cada teste por <see cref="IntegrationTestBase"/>.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        Clock.Reset();
        await SqlServerTestDatabase.ResetAsync();

        await using var scope = Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();

        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        _fixturePasswordHash ??= passwordHasher.HashPassword(DefaultPassword);

        await SeedFixtureUsersAsync(db, _fixturePasswordHash);
    }

    private static async Task SeedFixtureUsersAsync(SighDbContext db, string passwordHash)
    {
        var administratorRole = await db.Roles.SingleAsync(r => r.Name == DatabaseSeeder.AdministratorRoleName);

        var adminUser = CreateUser(AdminUserId, "Administrador do Sistema", "admin@sigh.com", "12345678901", passwordHash);

        // Segundo usuário com o mesmo perfil: necessário para fluxos que exigem
        // segregação de funções (quem registra a decisão não pode aprová-la).
        var approverUser = CreateUser(ApproverUserId, "Aprovador do Sistema", "approver@sigh.com", "55566677788", passwordHash);

        var lockedUser = CreateUser(LockedUserId, "Usuário Bloqueado", "locked@sigh.com", "98765432100", passwordHash);
        lockedUser.Status = UserStatus.Locked;
        lockedUser.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
        lockedUser.FailedLoginAttempts = 5;

        var unprivilegedUser = CreateUser(UnprivilegedUserId, "Usuário Sem Permissao", "unprivileged@sigh.com", "11122233344", passwordHash);

        db.Users.AddRange(adminUser, approverUser, lockedUser, unprivilegedUser);
        db.UserRoles.AddRange(
            new UserRole(Guid.NewGuid()) { UserId = adminUser.Id, RoleId = administratorRole.Id },
            new UserRole(Guid.NewGuid()) { UserId = approverUser.Id, RoleId = administratorRole.Id });

        await db.SaveChangesAsync();
    }

    private static User CreateUser(Guid id, string fullName, string email, string cpf, string passwordHash) =>
        new(id)
        {
            FullName = fullName,
            Email = email,
            Cpf = cpf,
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            MustChangePassword = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
