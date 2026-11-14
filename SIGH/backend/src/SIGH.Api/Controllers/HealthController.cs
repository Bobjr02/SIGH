using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;
using SIGH.Persistence.Context;

namespace SIGH.Api.Controllers;

[Route("api/v1/health")]
[Tags("Health")]
public class HealthController : BaseController
{
    private readonly SighDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<HealthController> _logger;

    public HealthController(SighDbContext dbContext, IWebHostEnvironment environment, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<HealthStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<HealthStatusResponse>>> GetHealthStatus(CancellationToken cancellationToken)
    {
        var isDbHealthy = false;

        try
        {
            isDbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Endpoint público: o detalhe do erro vai para o log, nunca para a resposta.
            _logger.LogWarning(ex, "Health: falha ao conectar no banco de dados.");
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
                    isDbHealthy ? "Conexão com o banco de dados estabelecida com sucesso." : "Falha de conexão com o banco de dados."
                )
            },
            // Só um booleano: o front mostra a faixa "AMBIENTE DE DEMONSTRAÇÃO" (Development ou Demo).
            Demo: SIGH.Api.Demo.DemoDataGuard.IsDemoEnvironment(_environment.EnvironmentName)
        );

        return OkResult(response, "Status de saúde do sistema obtido com sucesso.");
    }
}

public record HealthStatusResponse(
    string Status,
    DateTimeOffset Timestamp,
    Dictionary<string, HealthComponentStatus> Components,
    bool Demo
);

public record HealthComponentStatus(
    string Status,
    string Description
);
