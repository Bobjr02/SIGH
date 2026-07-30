namespace SIGH.Domain.Entities;

public class UserSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string? DeviceName { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    public UserSession() : base() { }

    public UserSession(Guid id) : base(id) { }
}
