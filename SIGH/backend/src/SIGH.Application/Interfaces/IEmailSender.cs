namespace SIGH.Application.Interfaces;

public sealed record OutgoingEmail(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>Entrega um e-mail ao servidor SMTP. Usado apenas pelo worker da outbox.</summary>
public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
