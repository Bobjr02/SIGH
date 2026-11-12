namespace SIGH.Application.Email;

/// <summary>
/// Enfileira um e-mail na outbox. Não grava sozinho: o e-mail entra no próximo SaveChanges do
/// caso de uso, na mesma transação da operação que o originou.
/// </summary>
public interface IEmailOutbox
{
    void Enqueue(string to, EmailContent content);
}
