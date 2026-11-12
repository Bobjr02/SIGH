using FluentAssertions;
using SIGH.Application.Email;
using Xunit;

namespace SIGH.UnitTests.Email;

public class EmailTemplatesTests
{
    private const string Link = "https://sigh.test/reset-password?token=abc123XYZ";

    [Fact]
    public void PasswordReset_HasLinkAndValidityInHtmlAndText()
    {
        var email = EmailTemplates.PasswordReset("Maria Souza", Link, 30);

        email.Template.Should().Be(EmailTemplates.PasswordResetTemplate);
        email.Subject.Should().Be("Redefinição de senha - SIGH");
        email.TextBody.Should().Contain("Olá, Maria Souza.").And.Contain(Link).And.Contain("30 minutos").And.Contain("uma única vez");
        email.HtmlBody.Should().Contain("lang=\"pt-BR\"").And.Contain("href=\"https://sigh.test/reset-password?token=abc123XYZ\"").And.Contain("30 minutos");
        email.TextBody.Should().Contain("ignore este e-mail");
    }

    [Fact]
    public void AccountCreated_ExplainsFirstAccessAndNeverContainsAPassword()
    {
        var email = EmailTemplates.AccountCreated("João Lima", "joao@sigh.test", Link + "&setup=1", 72, "https://sigh.test/login");

        email.Template.Should().Be(EmailTemplates.AccountCreatedTemplate);
        email.TextBody.Should().Contain("joao@sigh.test").And.Contain("defina sua senha").And.Contain("72 horas").And.Contain("https://sigh.test/login");
        email.HtmlBody.Should().Contain("Definir minha senha").And.Contain("&amp;setup=1");
        // O template nem recebe senha; e não há nenhum campo "Senha: xxx".
        email.TextBody.Should().NotMatchRegex("(?i)senha( inicial| provisória| temporária)?\\s*:\\s*\\S");
        email.HtmlBody.Should().NotMatchRegex("(?i)senha( inicial| provisória| temporária)?\\s*:\\s*\\S");
    }

    [Fact]
    public void DeadlineAlert_ShowsTitleMessageAndDueDateInBrasiliaTime()
    {
        var due = new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero); // 12:00 em Brasília

        var email = EmailTemplates.DeadlineAlert("Ana", "Prazo de defesa", "O prazo vence em 2 dias.", due, "https://sigh.test/notifications");

        email.Subject.Should().Be("Alerta de prazo - Prazo de defesa");
        email.TextBody.Should().Contain("O prazo vence em 2 dias.").And.Contain("20/11/2026 12:00").And.Contain("https://sigh.test/notifications");
        email.HtmlBody.Should().Contain("20/11/2026 12:00");
    }

    [Fact]
    public void DeadlineAlert_WithoutDueDate_OmitsTheLine()
    {
        var email = EmailTemplates.DeadlineAlert("Ana", "Lembrete", "Mensagem", null, "https://sigh.test/notifications");

        email.TextBody.Should().NotContain("Prazo:");
    }

    [Fact]
    public void UserProvidedValues_AreHtmlEncoded()
    {
        var hostile = "<script>alert('x')</script>";

        var reset = EmailTemplates.PasswordReset(hostile, Link, 30);
        var alert = EmailTemplates.DeadlineAlert(hostile, hostile, hostile, null, Link);

        reset.HtmlBody.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
        alert.HtmlBody.Should().NotContain("<script>");
        alert.Subject.Should().Contain(hostile, "o assunto é texto puro, não HTML");
    }
}
