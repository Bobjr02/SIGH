using Microsoft.Extensions.Logging;
using SIGH.Application.Interfaces;

namespace SIGH.Application.Authentication.ForgotPassword;

/// <summary>
/// "Esqueci a senha" no request: normaliza o e-mail e enfileira o pedido, SEMPRE. Não consulta o banco
/// nem monta e-mail aqui: o caminho e o tempo da resposta são os mesmos, exista ou não a conta.
/// O processamento fica com <see cref="PasswordResetRequestProcessor"/> (worker).
/// </summary>
public class ForgotPasswordService : IForgotPasswordService
{
    public const string GenericMessage =
        "Se o e-mail informado estiver cadastrado, você receberá as instruções para redefinir a senha.";

    private readonly IPasswordResetRequestQueue _queue;
    private readonly IRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ForgotPasswordService> _logger;

    public ForgotPasswordService(
        IPasswordResetRequestQueue queue,
        IRequestContext requestContext,
        IDateTimeProvider clock,
        ILogger<ForgotPasswordService> logger)
    {
        _queue = queue;
        _requestContext = requestContext;
        _clock = clock;
        _logger = logger;
    }

    public Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (!_queue.TryEnqueue(new PasswordResetRequestJob(email, _requestContext.IpAddress, _clock.UtcNow)))
        {
            // Não registra o e-mail informado: pode ser de terceiros.
            _logger.LogWarning("Fila de pedidos de redefinição de senha cheia: pedido descartado.");
        }

        return Task.FromResult(new ForgotPasswordResponse(true, GenericMessage));
    }
}
