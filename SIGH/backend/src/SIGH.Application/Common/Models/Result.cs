namespace SIGH.Application.Common.Models;

public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public Result() { }

    public Result(bool success, T? data, string? message = null, string? errorCode = null, Dictionary<string, string[]>? validationErrors = null)
    {
        Success = success;
        Data = data;
        Message = message;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Ok(T data, string? message = null)
    {
        return new Result<T>(true, data, message);
    }

    public static Result<T> Failure(string message, string? errorCode = null, Dictionary<string, string[]>? validationErrors = null)
    {
        return new Result<T>(false, default, message, errorCode, validationErrors);
    }
}

public class Result : Result<object>
{
    public static Result SuccessResult(string? message = null)
    {
        return new Result
        {
            Success = true,
            Data = null,
            Message = message
        };
    }

    public static Result FailureResult(string message, string? errorCode = null, Dictionary<string, string[]>? validationErrors = null)
    {
        return new Result
        {
            Success = false,
            Data = null,
            Message = message,
            ErrorCode = errorCode,
            ValidationErrors = validationErrors
        };
    }
}
