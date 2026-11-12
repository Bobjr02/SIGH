using System.Text.Json;
using SIGH.Application.Interfaces;
using SIGH.Application.Security;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Auditing;

public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly SighDbContext _context;
    private readonly IRequestContext _requestContext;
    private readonly ICurrentUserService _currentUser;

    public SecurityAuditLogger(SighDbContext context, IRequestContext requestContext, ICurrentUserService currentUser)
    {
        _context = context;
        _requestContext = requestContext;
        _currentUser = currentUser;
    }

    public async Task RecordAsync(
        string action,
        Guid? userId,
        object? details = null,
        CancellationToken cancellationToken = default,
        string? entityName = null,
        string? entityId = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Entity = entityName ?? nameof(User),
            EntityId = entityId ?? userId?.ToString(),
            NewValues = details is null ? null : JsonSerializer.Serialize(details, EntityAuditBuilder.JsonOptions),
            // Quem sofreu o evento (userId) pode não ser o autenticado: login com falha não tem token.
            UserId = userId ?? _currentUser.UserId,
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent,
            CorrelationId = _requestContext.CorrelationId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
