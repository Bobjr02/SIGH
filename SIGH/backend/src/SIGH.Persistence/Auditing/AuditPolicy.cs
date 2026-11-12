using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Auditing;

/// <summary>
/// O que entra na trilha de auditoria automática (SaveChanges) e o que nunca entra.
/// Ao criar uma entidade de negócio ou de segurança nova, avalie se ela entra em
/// <see cref="AuditedEntityTypes"/>; ao criar uma propriedade sensível, inclua-a em
/// <see cref="SensitiveProperties"/> (o teste AuditPolicyTests falha se uma propriedade com
/// nome de senha, token, segredo ou hash não estiver na lista).
/// </summary>
public static class AuditPolicy
{
    public const string Redacted = "[redacted]";

    /// <summary>Entidades auditadas: processos e subentidades, evidências, funcionários, usuários, perfis, permissões e tipos de infração.</summary>
    public static readonly IReadOnlySet<Type> AuditedEntityTypes = new HashSet<Type>
    {
        typeof(DisciplinaryCase),
        typeof(DisciplinaryOccurrence),
        typeof(DisciplinaryCaseEmployee),
        typeof(DisciplinaryEvidence),
        typeof(DisciplinaryDecision),
        typeof(DisciplinaryMeasure),
        typeof(InfractionType),
        typeof(Employee),
        typeof(User),
        typeof(UserRole),
        typeof(Role),
        typeof(Permission),
        typeof(RolePermission),
    };

    /// <summary>
    /// Lista de exclusão explícita: o valor NUNCA é gravado. Quando uma destas propriedades muda,
    /// a auditoria registra só que mudou, com o valor <see cref="Redacted"/>.
    /// Conteúdo de arquivo nunca passa pelo EF (fica no IFileStorage), então não aparece aqui.
    /// </summary>
    public static readonly IReadOnlySet<string> SensitiveProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "PasswordHash",
        "TokenHash",
        "ReplacedByTokenHash",
        "SecretKey",
        "Password",
        // Caminho físico do arquivo no storage: dado interno, não interessa à auditoria.
        "StorageReference",
        // Dado pessoal: a auditoria registra que o CPF mudou, sem o valor.
        "Cpf",
    };

    /// <summary>
    /// Propriedades cujo nome lembra segredo mas que NÃO são: o SHA-256 do arquivo de evidência é
    /// justamente a prova de integridade e precisa ficar na trilha; MustChangePassword é só um flag.
    /// </summary>
    public static readonly IReadOnlySet<string> NonSecretProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "Sha256Hash",
        "IntegrityHash",
        "MustChangePassword",
    };

    /// <summary>
    /// Metadados de auditoria (já ficam no próprio registro) e campos voláteis que mudam a cada
    /// login; não são listados como "campos alterados". O login tem eventos de segurança próprios.
    /// </summary>
    public static readonly IReadOnlySet<string> IgnoredProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "CreatedAt",
        "CreatedBy",
        "UpdatedAt",
        "UpdatedBy",
        "DeletedAt",
        "DeletedBy",
        "LastLoginAt",
        "FailedLoginAttempts",
    };

    public static bool IsAudited(Type clrType) => AuditedEntityTypes.Contains(clrType);
}
