using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Testcontainers.MsSql;

namespace SIGH.IntegrationTests.Infrastructure;

/// <summary>
/// Um único SQL Server (Testcontainers) por execução da suíte, com o schema criado pelas
/// migrations reais (MigrateAsync, nunca EnsureCreated). Entre testes, o Respawn apaga os dados
/// e preserva o catálogo criado pelo DatabaseSeeder (permissões, perfis e empresa padrão).
/// </summary>
public static class SqlServerTestDatabase
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string DatabaseName = "SIGH_IntegrationTests";

    /// <summary>
    /// Tabelas que o Respawn não limpa. Nenhuma delas tem FK para tabelas limpas.
    /// </summary>
    private static readonly Table[] PreservedTables =
    {
        new("__EFMigrationsHistory"),
        new("Permissions"),
        new("Roles"),
        new("RolePermissions"),
        new("Companies")
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static MsSqlContainer? _container;
    private static string? _connectionString;
    private static Respawner? _respawner;

    public static async Task<string> GetConnectionStringAsync()
    {
        if (_connectionString is not null)
            return _connectionString;

        await Gate.WaitAsync();
        try
        {
            if (_connectionString is not null)
                return _connectionString;

            var container = new MsSqlBuilder(Image).Build();

            await container.StartAsync();
            _container = container;

            // Ryuk remove o container ao fim do processo; isto é só uma garantia extra.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => container.DisposeAsync().AsTask().GetAwaiter().GetResult();

            var connectionString = new SqlConnectionStringBuilder(container.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
                TrustServerCertificate = true
            }.ConnectionString;

            var options = new DbContextOptionsBuilder<SighDbContext>()
                .UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(SighDbContext).Assembly.FullName))
                .Options;

            await using (var context = new SighDbContext(options))
            {
                await context.Database.MigrateAsync();
            }

            _connectionString = connectionString;
            return connectionString;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// Apaga os dados de teste. Empresas criadas pelos testes são removidas; a empresa padrão fica.
    /// </summary>
    public static async Task ResetAsync()
    {
        var connectionString = await GetConnectionStringAsync();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = new[] { "dbo" },
            TablesToIgnore = PreservedTables
        });

        await _respawner.ResetAsync(connection);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [dbo].[Companies] WHERE [Id] <> @defaultCompanyId";
        command.Parameters.AddWithValue("@defaultCompanyId", DatabaseSeeder.DefaultCompanyId);
        await command.ExecuteNonQueryAsync();
    }
}
