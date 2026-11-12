using Microsoft.EntityFrameworkCore;
using SIGH.Application.Email;
using SIGH.Application.Interfaces;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Email;

public sealed class EmailOutbox : IEmailOutbox
{
    private readonly SighDbContext _context;
    private readonly IDateTimeProvider _clock;

    public EmailOutbox(SighDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public void Enqueue(string to, EmailContent content) =>
        _context.EmailOutbox.Add(EmailOutboxMessage.Create(to, content.Subject, content.HtmlBody, content.TextBody, content.Template, _clock.UtcNow));
}

public sealed class EmailOutboxStore : IEmailOutboxStore
{
    private readonly SighDbContext _context;

    public EmailOutboxStore(SighDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmailOutboxMessage>> GetDueAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default) =>
        await _context.EmailOutbox
            .Where(m => m.Status == EmailOutboxStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
}
