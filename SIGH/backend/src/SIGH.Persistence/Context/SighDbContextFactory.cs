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

    // Fallback apenas para desenvolvimento local, compatível com o docker-compose.yml da raiz.
    private const string LocalDevelopmentConnectionString =
        "Server=localhost,1433;Database=SIGH_DB;User Id=sa;Password=YourStrong@Password2026!;TrustServerCertificate=True;";

    public SighDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(SighDbContext).Assembly.FullName))
            .Options;

        return new SighDbContext(options);
    }
}
