using SIGH.Application.Common.Models;

namespace SIGH.Application.Audit;

public record AuditLogFilter(
    string? EntityName = null,
    string? EntityId = null,
    Guid? UserId = null,
    string? Action = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 50);

public record AuditLogDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Action,
    string Entity,
    string? EntityId,
    Guid? UserId,
    string? UserName,
    string? IpAddress,
    string? CorrelationId,
    string? OldValues,
    string? NewValues);

/// <summary>Consulta somente leitura da trilha de auditoria.</summary>
public interface IAuditLogQueryService
{
    public const int MaxPageSize = 200;

    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Linha do tempo de um processo: registros do processo e das subentidades (ocorrências,
    /// envolvidos, evidências — inclusive removidas —, decisões e medidas), mais recentes primeiro.
    /// Null se o processo não existir.
    /// </summary>
    Task<PagedResult<AuditLogDto>?> GetDisciplinaryCaseTrailAsync(Guid caseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
