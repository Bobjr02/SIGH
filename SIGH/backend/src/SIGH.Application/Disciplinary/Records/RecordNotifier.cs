using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Email;
using SIGH.Application.Options;
using SIGH.Domain.Enums;
using SIGH.Domain.Notifications.Entities;

namespace SIGH.Application.Disciplinary.Records;

/// <summary>
/// Avisos das medidas: no sistema (Notification) e, se Records:NotifyByEmail, por e-mail pela outbox.
/// Nada é gravado aqui: tudo entra no SaveChanges da operação (mesma transação).
/// As mensagens não trazem nome nem CPF do funcionário: só o número e o tipo da medida.
/// </summary>
public interface IRecordNotifier
{
    Task RecordRegisteredAsync(Guid companyId, Guid recordId, string caseNumber, string measureLabel, Guid registrantUserId, bool resubmitted, CancellationToken cancellationToken);
    Task RecordReviewedAsync(Guid companyId, Guid recordId, string caseNumber, string measureLabel, Guid registrantUserId, bool approved, string? reason, CancellationToken cancellationToken);
}

public sealed class RecordNotifier : IRecordNotifier
{
    public const string RegisteredType = "RecordRegistered";
    public const string ApprovedType = "RecordApproved";
    public const string RejectedType = "RecordRejected";
    public const string PendingDigestType = "RecordPendingApproval";

    private readonly IApplicationDbContext _db;
    private readonly IEmailOutbox _outbox;
    private readonly RecordsOptions _options;
    private readonly AppOptions _app;

    public RecordNotifier(IApplicationDbContext db, IEmailOutbox outbox, IOptions<RecordsOptions> options, IOptions<AppOptions> app)
    {
        _db = db;
        _outbox = outbox;
        _options = options.Value;
        _app = app.Value;
    }

    /// <summary>Usuários ativos com a permissão de aprovar (perfil vigente agora).</summary>
    public static IQueryable<SIGH.Domain.Entities.User> Approvers(IApplicationDbContext db, DateTimeOffset now) =>
        db.Users.Where(u => u.Status == UserStatus.Active
                            && u.UserRoles.Any(ur => !ur.IsDeleted
                                                     && (ur.ValidFrom == null || ur.ValidFrom <= now)
                                                     && (ur.ValidUntil == null || ur.ValidUntil > now)
                                                     && ur.Role.RolePermissions.Any(rp => !rp.IsDeleted && rp.Permission.Code == PermissionCodes.DisciplinaryRecords.Approve)));

    public async Task RecordRegisteredAsync(Guid companyId, Guid recordId, string caseNumber, string measureLabel, Guid registrantUserId, bool resubmitted, CancellationToken cancellationToken)
    {
        var approvers = await Approvers(_db, DateTimeOffset.UtcNow)
            .Where(u => u.Id != registrantUserId)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        var title = resubmitted ? "Medida corrigida aguardando aprovação" : "Nova medida aguardando aprovação";
        var message = $"{measureLabel} ({caseNumber}) {(resubmitted ? "foi corrigida e reenviada" : "foi registrada")} e aguarda aprovação.";
        foreach (var approver in approvers)
            Notify(companyId, approver.Id, approver.FullName, approver.Email, title, message, RegisteredType, recordId);
    }

    public async Task RecordReviewedAsync(Guid companyId, Guid recordId, string caseNumber, string measureLabel, Guid registrantUserId, bool approved, string? reason, CancellationToken cancellationToken)
    {
        var registrant = await _db.Users.Where(u => u.Id == registrantUserId)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (registrant is null)
            return;

        var title = approved ? "Medida aprovada" : "Medida rejeitada";
        var message = approved
            ? $"{measureLabel} ({caseNumber}) foi aprovada."
            : $"{measureLabel} ({caseNumber}) foi rejeitada. Motivo: {reason}. Corrija e reenvie, ou cancele.";
        Notify(companyId, registrant.Id, registrant.FullName, registrant.Email, title, message, approved ? ApprovedType : RejectedType, recordId);
    }

    public void Notify(Guid companyId, Guid userId, string fullName, string email, string title, string message, string type, Guid? recordId)
    {
        _db.Notifications.Add(Notification.Create(companyId, userId, title, message, type, "Medium", disciplinaryCaseId: recordId));

        if (_options.NotifyByEmail && !string.IsNullOrWhiteSpace(email))
        {
            var link = _app.BuildUrl(recordId is { } id ? $"/medidas/{id}" : "/medidas/aprovacoes");
            _outbox.Enqueue(email, EmailTemplates.RecordNotice(fullName, title, message, link));
        }
    }
}
