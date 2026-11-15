using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace SIGH.Api.Localization;

/// <summary>
/// Títulos e mensagens padrão do ASP.NET Core em português. Os controllers do SIGH já escrevem as próprias
/// mensagens; aqui ficam as que o framework gera sozinho (validação de modelo, 404, 401, 403 etc.).
/// </summary>
public static partial class PortugueseProblemDetails
{
    /// <summary>Título em português por status HTTP.</summary>
    public static readonly IReadOnlyDictionary<int, string> TitlesByStatus = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "Requisição inválida",
        [StatusCodes.Status401Unauthorized] = "Não autorizado",
        [StatusCodes.Status403Forbidden] = "Acesso negado",
        [StatusCodes.Status404NotFound] = "Não encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método não permitido",
        [StatusCodes.Status406NotAcceptable] = "Formato não aceito",
        [StatusCodes.Status408RequestTimeout] = "Tempo de requisição esgotado",
        [StatusCodes.Status409Conflict] = "Conflito",
        [StatusCodes.Status412PreconditionFailed] = "Pré-condição não atendida",
        [StatusCodes.Status413PayloadTooLarge] = "Arquivo muito grande",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de conteúdo não suportado",
        [StatusCodes.Status422UnprocessableEntity] = "Dados não processáveis",
        [StatusCodes.Status429TooManyRequests] = "Muitas requisições",
        [StatusCodes.Status500InternalServerError] = "Erro interno no servidor",
        [StatusCodes.Status503ServiceUnavailable] = "Serviço indisponível",
    };

    public const string ValidationTitle = "Um ou mais erros de validação ocorreram.";

    /// <summary>Títulos que o ASP.NET Core escreve em inglês (ValidationProblemDetails e ClientErrorMapping).</summary>
    private static readonly HashSet<string> EnglishTitles = new(StringComparer.Ordinal)
    {
        "One or more validation errors occurred.", "Bad Request", "Unauthorized", "Forbidden", "Not Found",
        "Method Not Allowed", "Not Acceptable", "Request Timeout", "Conflict", "Precondition Failed", "Payload Too Large",
        "Unsupported Media Type", "Unprocessable Entity", "Too Many Requests", "Internal Server Error", "Service Unavailable",
        "An error occurred while processing your request.",
    };

    /// <summary>ProblemDetailsOptions.CustomizeProblemDetails: troca títulos e mensagens padrão em inglês.</summary>
    public static void Customize(ProblemDetails problem)
    {
        if (problem.Title is null || EnglishTitles.Contains(problem.Title))
        {
            problem.Title = problem is ValidationProblemDetails
                ? ValidationTitle
                : problem.Status is { } status && TitlesByStatus.TryGetValue(status, out var title) ? title : problem.Title;
        }

        if (problem is ValidationProblemDetails validation)
        {
            foreach (var key in validation.Errors.Keys.ToList())
                validation.Errors[key] = validation.Errors[key].Select(TranslateMessage).ToArray();
        }
    }

    /// <summary>ApiBehaviorOptions: os títulos dos erros de cliente (404, 401…) já saem em português.</summary>
    public static void Configure(ApiBehaviorOptions options)
    {
        foreach (var (status, mapping) in options.ClientErrorMapping)
        {
            if (TitlesByStatus.TryGetValue(status, out var title))
                mapping.Title = title;
        }
    }

    /// <summary>Mensagens de binding de modelo (valores inválidos, campos ausentes) em português.</summary>
    public static void Configure(DefaultModelBindingMessageProvider p)
    {
        p.SetAttemptedValueIsInvalidAccessor((value, field) => $"O valor '{value}' não é válido para {field}.");
        p.SetMissingBindRequiredValueAccessor(field => $"O campo {field} é obrigatório.");
        p.SetMissingKeyOrValueAccessor(() => "Um valor é obrigatório.");
        p.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"O valor '{value}' não é válido.");
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado não é válido.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "O campo precisa ser um número.");
        p.SetUnknownValueIsInvalidAccessor(field => $"O valor informado não é válido para {field}.");
        p.SetValueIsInvalidAccessor(value => $"O valor '{value}' é inválido.");
        p.SetValueMustBeANumberAccessor(field => $"O campo {field} precisa ser um número.");
        p.SetValueMustNotBeNullAccessor(value => $"O valor '{value}' não pode ser vazio.");
    }

    /// <summary>Traduz as mensagens padrão que ainda vêm do framework (atributos implícitos e JSON).</summary>
    public static string TranslateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        var required = RequiredField().Match(message);
        if (required.Success)
            return $"O campo {required.Groups[1].Value} é obrigatório.";
        if (message.StartsWith("The JSON value could not be converted", StringComparison.Ordinal)
            || message.StartsWith("'", StringComparison.Ordinal) && message.Contains("is an invalid", StringComparison.Ordinal))
            return "Valor inválido.";
        if (message.StartsWith("A non-empty request body is required", StringComparison.Ordinal))
            return "O corpo da requisição é obrigatório.";
        if (message.StartsWith("The ", StringComparison.Ordinal) && message.All(c => c < 128))
            return "Valor inválido.";
        return message;
    }

    [GeneratedRegex(@"^The (.+) field is required\.$")]
    private static partial Regex RequiredField();
}
