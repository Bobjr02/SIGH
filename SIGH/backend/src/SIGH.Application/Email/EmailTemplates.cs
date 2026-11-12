using System.Globalization;
using System.Net;
using System.Text;

namespace SIGH.Application.Email;

/// <summary>
/// Templates de e-mail em pt-BR, com versão HTML e texto. Sem engine de templates: todo valor
/// variável passa por <see cref="WebUtility.HtmlEncode"/> na versão HTML. Nenhum template recebe senha.
/// </summary>
public static class EmailTemplates
{
    public const string PasswordResetTemplate = "PasswordReset";
    public const string AccountCreatedTemplate = "AccountCreated";
    public const string DeadlineAlertTemplate = "DeadlineAlert";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly TimeZoneInfo Brasilia = FindBrasilia();

    public static EmailContent PasswordReset(string fullName, string resetLink, int validMinutes)
    {
        const string subject = "Redefinição de senha - SIGH";

        var text = new StringBuilder()
            .AppendLine($"Olá, {fullName}.")
            .AppendLine()
            .AppendLine("Recebemos um pedido para redefinir a senha da sua conta no SIGH.")
            .AppendLine($"Para criar uma nova senha, acesse o link abaixo (válido por {validMinutes} minutos e utilizável uma única vez):")
            .AppendLine()
            .AppendLine(resetLink)
            .AppendLine()
            .AppendLine("Se você não fez esse pedido, ignore este e-mail: sua senha atual continua valendo.")
            .ToString();

        var html = Layout(subject,
            $"<p>Olá, {E(fullName)}.</p>" +
            "<p>Recebemos um pedido para redefinir a senha da sua conta no SIGH.</p>" +
            Button(resetLink, "Redefinir senha") +
            $"<p>O link vale por <strong>{validMinutes} minutos</strong> e pode ser usado uma única vez.</p>" +
            LinkFallback(resetLink) +
            "<p>Se você não fez esse pedido, ignore este e-mail: sua senha atual continua valendo.</p>");

        return new EmailContent(PasswordResetTemplate, subject, html, text);
    }

    /// <summary>
    /// Conta criada. NUNCA contém a senha: o usuário define a própria senha pelo link (token de uso
    /// único, válido por <paramref name="validHours"/> horas).
    /// </summary>
    public static EmailContent AccountCreated(string fullName, string email, string setupLink, int validHours, string loginUrl)
    {
        const string subject = "Sua conta no SIGH foi criada";

        var text = new StringBuilder()
            .AppendLine($"Olá, {fullName}.")
            .AppendLine()
            .AppendLine("Uma conta foi criada para você no SIGH - Sistema Inteligente de Gestão de Histórico Disciplinar.")
            .AppendLine($"Usuário (e-mail): {email}")
            .AppendLine()
            .AppendLine("Primeiro acesso: defina sua senha pelo link abaixo.")
            .AppendLine($"O link vale por {validHours} horas e pode ser usado uma única vez:")
            .AppendLine()
            .AppendLine(setupLink)
            .AppendLine()
            .AppendLine($"Depois, entre em {loginUrl} com o seu e-mail e a senha que você definiu.")
            .AppendLine("Se o link expirar, use \"Esqueci minha senha\" na tela de login.")
            .ToString();

        var html = Layout(subject,
            $"<p>Olá, {E(fullName)}.</p>" +
            "<p>Uma conta foi criada para você no <strong>SIGH</strong> - Sistema Inteligente de Gestão de Histórico Disciplinar.</p>" +
            $"<p>Usuário (e-mail): <strong>{E(email)}</strong></p>" +
            "<p><strong>Primeiro acesso:</strong> defina sua senha pelo botão abaixo.</p>" +
            Button(setupLink, "Definir minha senha") +
            $"<p>O link vale por <strong>{validHours} horas</strong> e pode ser usado uma única vez.</p>" +
            LinkFallback(setupLink) +
            $"<p>Depois, entre em <a href=\"{E(loginUrl)}\">{E(loginUrl)}</a> com o seu e-mail e a senha que você definiu. " +
            "Se o link expirar, use &quot;Esqueci minha senha&quot; na tela de login.</p>");

        return new EmailContent(AccountCreatedTemplate, subject, html, text);
    }

    public static EmailContent DeadlineAlert(string fullName, string title, string message, DateTimeOffset? dueDate, string link)
    {
        var subject = $"Alerta de prazo - {title}";
        var due = dueDate.HasValue ? TimeZoneInfo.ConvertTime(dueDate.Value, Brasilia).ToString("dd/MM/yyyy HH:mm", PtBr) : null;

        var text = new StringBuilder()
            .AppendLine($"Olá, {fullName}.")
            .AppendLine()
            .AppendLine(title)
            .AppendLine(message);
        if (due is not null)
            text.AppendLine($"Prazo: {due} (horário de Brasília)");
        text.AppendLine()
            .AppendLine($"Acompanhe no SIGH: {link}");

        var html = Layout(subject,
            $"<p>Olá, {E(fullName)}.</p>" +
            $"<p><strong>{E(title)}</strong></p>" +
            $"<p>{E(message)}</p>" +
            (due is null ? string.Empty : $"<p>Prazo: <strong>{E(due)}</strong> (horário de Brasília)</p>") +
            Button(link, "Abrir no SIGH") +
            LinkFallback(link));

        return new EmailContent(DeadlineAlertTemplate, subject, html, text.ToString());
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);

    private static string Button(string href, string label) =>
        $"<p><a href=\"{E(href)}\" style=\"display:inline-block;padding:10px 18px;background:#1e40af;color:#ffffff;" +
        $"text-decoration:none;border-radius:6px;font-weight:600\">{E(label)}</a></p>";

    private static string LinkFallback(string href) =>
        $"<p style=\"font-size:12px;color:#64748b\">Se o botão não funcionar, copie e cole este endereço no navegador:<br>{E(href)}</p>";

    private static string Layout(string title, string body) =>
        "<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">" +
        $"<title>{E(title)}</title></head>" +
        "<body style=\"font-family:Arial,Helvetica,sans-serif;color:#1e293b;line-height:1.5\">" +
        "<div style=\"max-width:560px;margin:0 auto;padding:24px\">" +
        body +
        "<hr style=\"border:none;border-top:1px solid #e2e8f0\">" +
        "<p style=\"font-size:12px;color:#64748b\">Mensagem automática do SIGH. Não responda este e-mail.</p>" +
        "</div></body></html>";

    private static TimeZoneInfo FindBrasilia()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
                return tz;
        }
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
