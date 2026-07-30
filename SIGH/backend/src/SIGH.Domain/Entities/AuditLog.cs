namespace SIGH.Domain.Entities;

public class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }

    public AuditLog() : base() { }

    public AuditLog(Guid id) : base(id) { }
}
