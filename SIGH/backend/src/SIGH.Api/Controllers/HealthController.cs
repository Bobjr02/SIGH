using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Persistence.Context;

namespace SIGH.Api.Controllers;

[Route("api/v1/health")]
[Tags("Health")]
public class HealthController : BaseController
{
    private readonly SighDbContext _dbContext;

    public HealthController(SighDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<HealthStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<HealthStatusResponse>>> GetHealthStatus(CancellationToken cancellationToken)
    {
        var isDbHealthy = false;
        string? dbError = null;

        try
        {
            isDbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        var status = isDbHealthy ? "Healthy" : "Degraded";

        var response = new HealthStatusResponse(
            Status: status,
            Timestamp: DateTimeOffset.UtcNow,
            Components: new Dictionary<string, HealthComponentStatus>
            {
                ["Api"] = new HealthComponentStatus("Healthy", "API do SIGH está operacional."),
                ["Database"] = new HealthComponentStatus(
                    isDbHealthy ? "Healthy" : "Unhealthy",
                    isDbHealthy ? "Conexão com o banco de dados estabelecida com sucesso." : $"Falha de conexão com o banco de dados: {dbError}"
                )
            }
        );

        return OkResult(response, "Status de saúde do sistema obtido com sucesso.");
    }
}

public record HealthStatusResponse(
    string Status,
    DateTimeOffset Timestamp,
    Dictionary<string, HealthComponentStatus> Components
);

public record HealthComponentStatus(
    string Status,
    string Description
);
