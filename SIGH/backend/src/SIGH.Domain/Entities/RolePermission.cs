namespace SIGH.Domain.Entities;

public class RolePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;

    public RolePermission() : base() { }

    public RolePermission(Guid id) : base(id) { }
}
