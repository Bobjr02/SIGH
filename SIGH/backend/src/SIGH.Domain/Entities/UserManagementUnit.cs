using SIGH.Domain.Employees.Entities;

namespace SIGH.Domain.Entities;

/// <summary>
/// Gerência (Chave Organização) pela qual o usuário responde. Define o escopo de consulta de quem
/// não tem a permissão de escopo total. Uma pessoa pode gerir várias gerências.
/// </summary>
public class UserManagementUnit : AuditableEntity, IHasValidity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid ManagementUnitId { get; set; }
    public virtual ManagementUnit ManagementUnit { get; set; } = null!;

    /// <summary>Início da vigência (inclusivo). Nulo: desde sempre.</summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>Fim da vigência (exclusivo). Nulo: sem prazo.</summary>
    public DateTimeOffset? ValidUntil { get; set; }

    public UserManagementUnit() : base() { }

    public UserManagementUnit(Guid id) : base(id) { }
}
