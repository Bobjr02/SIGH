namespace SIGH.Domain.Entities;

public class Permission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public Permission() : base() { }

    public Permission(Guid id) : base(id) { }
}
