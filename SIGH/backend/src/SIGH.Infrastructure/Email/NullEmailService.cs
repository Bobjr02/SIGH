using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Email;

public class NullEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Implementação neutra no-op
        return Task.CompletedTask;
    }
}
