namespace SIGH.Domain.Entities;

public class PasswordHistory : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string PasswordHash { get; set; } = string.Empty;

    public PasswordHistory() : base() { }

    public PasswordHistory(Guid id) : base(id) { }
}
