using SIGH.Application.Interfaces;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.ForgotPassword;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly IUserRepository _userRepository;
    private readonly IRandomStringGenerator _randomStringGenerator;
    private readonly IEmailService _emailService;

    public ForgotPasswordService(
        IUserRepository userRepository,
        IRandomStringGenerator randomStringGenerator,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _randomStringGenerator = randomStringGenerator;
        _emailService = emailService;
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user != null)
        {
            var resetToken = _randomStringGenerator.GenerateRandomString(32);
            var subject = "Redefinição de Senha - SIGH";
            var body = $"Olá {user.FullName}, utilize o token/código a seguir para redefinir sua senha: {resetToken}";

            await _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
        }

        // Sempre retorna mensagem genérica para mitigar enumeração de contas
        return new ForgotPasswordResponse(true, "Se o e-mail informado estiver cadastrado em nosso sistema, as instruções para redefinição de senha foram enviadas.");
    }
}
