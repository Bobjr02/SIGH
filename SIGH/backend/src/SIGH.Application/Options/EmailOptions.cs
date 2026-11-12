namespace SIGH.Application.Options;

/// <summary>
/// E-mail (seção "Email"). A senha SMTP só pode vir de user-secrets ou variável de ambiente
/// (Email__Smtp__Password), nunca de appsettings versionado.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Com false, nada é enviado (NullEmailSender, com aviso no startup); a outbox continua sendo gravada.</summary>
    public bool Enabled { get; set; }
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "SIGH";
    /// <summary>Envia e-mail quando o worker de prazos gera uma notificação.</summary>
    public bool SendDeadlineAlerts { get; set; } = true;
    public SmtpOptions Smtp { get; set; } = new();
    public EmailOutboxOptions Outbox { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseStartTls { get; set; } = true;
}

public class EmailOutboxOptions
{
    /// <summary>O worker de envio roda? (desligado nos testes de integração).</summary>
    public bool DispatcherEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 5;
    /// <summary>Espera antes da 2ª tentativa; dobra a cada falha (backoff exponencial), até <see cref="MaxRetryDelaySeconds"/>.</summary>
    public int InitialRetryDelaySeconds { get; set; } = 30;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
}
