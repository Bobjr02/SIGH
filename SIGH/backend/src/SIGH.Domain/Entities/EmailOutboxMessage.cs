using SIGH.Domain.Common;

namespace SIGH.Domain.Entities;

public enum EmailOutboxStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

/// <summary>
/// E-mail a enviar (padrão outbox): gravado na mesma transação da operação que o originou e enviado
/// depois por um worker, com retentativas. Nenhuma requisição HTTP espera o SMTP.
/// </summary>
public class EmailOutboxMessage : Entity
{
    public const int MaxErrorLength = 2000;
    public const string RedactedBody = "[conteúdo removido após o envio]";

    public string To { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    /// <summary>Nome do template (ex.: PasswordReset), para consulta e métricas.</summary>
    public string Template { get; private set; } = string.Empty;
    public EmailOutboxStatus Status { get; private set; } = EmailOutboxStatus.Pending;
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? LastError { get; private set; }

    protected EmailOutboxMessage() { }

    public static EmailOutboxMessage Create(string to, string subject, string htmlBody, string textBody, string template, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Destinatário obrigatório.", nameof(to));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Assunto obrigatório.", nameof(subject));

        return new EmailOutboxMessage
        {
            To = to.Trim(),
            Subject = subject.Trim(),
            HtmlBody = htmlBody,
            TextBody = textBody,
            Template = template,
            CreatedAt = now,
            NextAttemptAt = now,
            Status = EmailOutboxStatus.Pending
        };
    }

    /// <summary>
    /// Enviado. O corpo é apagado: pode conter link com token (redefinição de senha), e não há
    /// motivo para guardá-lo no banco depois de entregue ao servidor SMTP.
    /// </summary>
    public void MarkSent(DateTimeOffset now)
    {
        Attempts++;
        LastAttemptAt = now;
        SentAt = now;
        Status = EmailOutboxStatus.Sent;
        LastError = null;
        HtmlBody = RedactedBody;
        TextBody = RedactedBody;
    }

    /// <summary>Falha numa tentativa: agenda a próxima ou, no limite, marca como falha definitiva.</summary>
    public void MarkAttemptFailed(string error, DateTimeOffset now, int maxAttempts, TimeSpan nextDelay)
    {
        Attempts++;
        LastAttemptAt = now;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;

        if (Attempts >= maxAttempts)
        {
            Status = EmailOutboxStatus.Failed;
            HtmlBody = RedactedBody;
            TextBody = RedactedBody;
        }
        else
        {
            NextAttemptAt = now + nextDelay;
        }
    }
}
