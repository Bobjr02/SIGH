namespace SIGH.Application.Email;

/// <summary>E-mail pronto: assunto e corpo em HTML e em texto puro.</summary>
public sealed record EmailContent(string Template, string Subject, string HtmlBody, string TextBody);
