using System.Linq;
using SIGH.Application.Options;

namespace SIGH.Application.Common.Validators;

public static class PasswordComplexityValidator
{
    public static (bool IsValid, string ErrorMessage) Validate(string password, PasswordOptions options)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "A senha não pode ser vazia.");

        if (password.Length < options.RequiredLength)
            return (false, $"A senha deve conter no mínimo {options.RequiredLength} caracteres.");

        if (options.RequireDigit && !password.Any(char.IsDigit))
            return (false, "A senha deve conter pelo menos um número.");

        if (options.RequireLowercase && !password.Any(char.IsLower))
            return (false, "A senha deve conter pelo menos uma letra minúscula.");

        if (options.RequireUppercase && !password.Any(char.IsUpper))
            return (false, "A senha deve conter pelo menos uma letra maiúscula.");

        if (options.RequireNonAlphanumeric && !password.Any(ch => !char.IsLetterOrDigit(ch)))
            return (false, "A senha deve conter pelo menos um caractere especial (ex: @, #, $, %).");

        return (true, string.Empty);
    }
}
