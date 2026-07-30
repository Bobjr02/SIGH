using System.Net;
using System.Security;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SIGH.Domain.Exceptions;

namespace SIGH.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção capturada pelo GlobalExceptionMiddleware: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["requestId"] = context.TraceIdentifier }
        };

        if (context.Response.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        switch (exception)
        {
            case BusinessRuleValidationException businessEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Violação de Regra de Negócio";
                problemDetails.Detail = businessEx.Message;
                if (!string.IsNullOrEmpty(businessEx.Details))
                {
                    problemDetails.Extensions["details"] = businessEx.Details;
                }
                break;

            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Erro de Validação de Parâmetros";
                problemDetails.Detail = "Um ou mais erros de validação ocorreram.";

                var errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                problemDetails.Extensions["validationErrors"] = errors;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Title = "Não Autorizado";
                problemDetails.Detail = string.IsNullOrWhiteSpace(unauthorizedEx.Message)
                    ? "Acesso não autorizado ao recurso solicitado."
                    : unauthorizedEx.Message;
                break;

            case SecurityException securityEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Acesso Proibido";
                problemDetails.Detail = string.IsNullOrWhiteSpace(securityEx.Message)
                    ? "Permissões insuficientes para executar esta operação."
                    : securityEx.Message;
                break;

            case KeyNotFoundException keyNotFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Recurso Não Encontrado";
                problemDetails.Detail = keyNotFoundEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Erro Interno no Servidor";
                problemDetails.Detail = "Ocorreu um erro inesperado no processamento da solicitação.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);

        return context.Response.WriteAsync(json);
    }
}
