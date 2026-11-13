using System.Threading.Channels;

namespace SIGH.Application.Authentication.ForgotPassword;

/// <summary>Pedido de "esqueci a senha" a ser processado fora do request.</summary>
public sealed record PasswordResetRequestJob(string Email, string? IpAddress, DateTimeOffset RequestedAt);

/// <summary>
/// Fila dos pedidos de redefinição. O request só enfileira (sempre, exista ou não o e-mail);
/// quem decide se há conta, gera o token e enfileira o e-mail é o worker. Assim o tempo e o caminho
/// do request não dependem de o e-mail existir.
/// </summary>
public interface IPasswordResetRequestQueue
{
    /// <summary>Enfileira o pedido. Devolve false se a fila estiver cheia (o pedido é descartado e registrado em log).</summary>
    bool TryEnqueue(PasswordResetRequestJob job);

    IAsyncEnumerable<PasswordResetRequestJob> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Fila em memória, com limite. Escolha consciente: não grava no banco e-mails informados por
/// terceiros (que podem nem existir). Um pedido ainda não processado se perde se a API reiniciar;
/// o usuário só precisa pedir de novo.
/// </summary>
public sealed class InMemoryPasswordResetRequestQueue : IPasswordResetRequestQueue
{
    public const int Capacity = 1000;

    private readonly Channel<PasswordResetRequestJob> _channel = Channel.CreateBounded<PasswordResetRequestJob>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public bool TryEnqueue(PasswordResetRequestJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<PasswordResetRequestJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
