namespace SIGH.Domain.Notifications.Entities;

using SIGH.Domain.Common;

public class Notification : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = "Info";
    public string Priority { get; private set; } = "Medium";
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsExpired { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string? Metadata { get; private set; }

    /// <summary>
    /// Processo/medida a que a notificação se refere. Usado para o escopo por gerência: quem perdeu
    /// o acesso ao funcionário deixa de ver a notificação.
    /// </summary>
    public Guid? DisciplinaryCaseId { get; private set; }

    // EF Core constructor
    protected Notification() : base() { }

    public Notification(
        Guid companyId,
        Guid userId,
        string title,
        string message,
        string type = "Info",
        string priority = "Medium",
        DateTimeOffset? dueDate = null,
        string? metadata = null,
        Guid? disciplinaryCaseId = null) : base()
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId é obrigatório.", nameof(companyId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem é obrigatória.", nameof(message));

        CompanyId = companyId;
        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Type = string.IsNullOrWhiteSpace(type) ? "Info" : type.Trim();
        Priority = string.IsNullOrWhiteSpace(priority) ? "Medium" : priority.Trim();
        DueDate = dueDate;
        Metadata = metadata;
        // O vínculo com o processo é o que o escopo por gerência usa: se não vier explícito, sai do Metadata
        // ("SourceEntity=DisciplinaryCase;SourceId=<id>", usado pelos alertas de prazo).
        DisciplinaryCaseId = disciplinaryCaseId ?? CaseIdFromMetadata(metadata);
        IsRead = false;
        ReadAt = null;
        IsExpired = false;
    }

    public static Notification Create(
        Guid companyId,
        Guid userId,
        string title,
        string message,
        string type = "Info",
        string priority = "Medium",
        DateTimeOffset? dueDate = null,
        string? metadata = null,
        Guid? disciplinaryCaseId = null)
    {
        return new Notification(companyId, userId, title, message, type, priority, dueDate, metadata, disciplinaryCaseId);
    }

    /// <summary>
    /// Id do processo referenciado no Metadata (formato chave=valor separado por ";"):
    /// SourceEntity=DisciplinaryCase + SourceId=&lt;guid&gt;, ou DisciplinaryCaseId=&lt;guid&gt;.
    /// </summary>
    public static Guid? CaseIdFromMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        var pairs = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First()[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (pairs.TryGetValue("DisciplinaryCaseId", out var direct) && Guid.TryParse(direct, out var directId))
            return directId;

        return pairs.TryGetValue("SourceEntity", out var entity)
               && string.Equals(entity, "DisciplinaryCase", StringComparison.OrdinalIgnoreCase)
               && pairs.TryGetValue("SourceId", out var source)
               && Guid.TryParse(source, out var sourceId)
            ? sourceId
            : null;
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = readAt;
            UpdatedAt = readAt;
        }
    }

    public void MarkAsExpired()
    {
        if (!IsExpired)
        {
            IsExpired = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
