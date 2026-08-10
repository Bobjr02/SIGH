using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SIGH.Application.Common.Models;

namespace SIGH.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido.");
        }
        return userId;
    }

    protected ActionResult<Result<T>> OkResult<T>(T data, string? message = null)
    {
        return Ok(Result<T>.Ok(data, message));
    }

    protected ActionResult<Result<T>> CreatedResult<T>(string routeName, object? routeValues, T data, string? message = null)
    {
        return CreatedAtRoute(routeName, routeValues, Result<T>.Ok(data, message));
    }

    protected ActionResult<Result<T>> BadRequestResult<T>(string message, string? errorCode = null, Dictionary<string, string[]>? validationErrors = null)
    {
        return BadRequest(Result<T>.Failure(message, errorCode, validationErrors));
    }

    protected ActionResult<Result<T>> NotFoundResult<T>(string message, string? errorCode = null)
    {
        return NotFound(Result<T>.Failure(message, errorCode));
    }

    protected ActionResult<Result<T>> ConflictResult<T>(Result<T> result)
    {
        return Conflict(result);
    }

    protected static bool IsConflictErrorCode(string? errorCode)
    {
        return !string.IsNullOrWhiteSpace(errorCode)
            && errorCode.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase);
    }

    protected ActionResult<Result<T>> UnauthorizedResult<T>(string message = "Acesso não autorizado.", string? errorCode = null)
    {
        return StatusCode(401, Result<T>.Failure(message, errorCode));
    }

    protected ActionResult<Result<T>> ForbiddenResult<T>(string message = "Acesso proibido.", string? errorCode = null)
    {
        return StatusCode(403, Result<T>.Failure(message, errorCode));
    }
}
