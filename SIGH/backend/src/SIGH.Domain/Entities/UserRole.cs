namespace SIGH.Domain.Entities;

public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public UserRole() : base() { }

    public UserRole(Guid id) : base(id) { }
}
