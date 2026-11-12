using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIGH.Persistence.Context;

/// <summary>
/// Fábrica usada apenas em tempo de design pelas ferramentas do EF Core
/// (dotnet ef migrations / database update). Não é usada pela aplicação em execução.
/// </summary>
public class SighDbContextFactory : IDesignTimeDbContextFactory<SighDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__DefaultConnection";

    // Fallback sem credenciais: basta para comandos que não abrem conexão
    // (migrations add, has-pending-model-changes). Para "database update",
    // defina ConnectionStrings__DefaultConnection com a connection string completa.
    private const string DesignTimeFallbackConnectionString =
        "Server=localhost,1433;Database=SIGH_DB;TrustServerCertificate=True;";

    public SighDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = DesignTimeFallbackConnectionString;

        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(SighDbContext).Assembly.FullName))
            .Options;

        return new SighDbContext(options);
    }
}
