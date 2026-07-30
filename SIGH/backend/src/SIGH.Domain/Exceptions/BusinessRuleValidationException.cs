namespace SIGH.Domain.Exceptions;

public class BusinessRuleValidationException : Exception
{
    public string Details { get; }

    public BusinessRuleValidationException(string message)
        : base(message)
    {
        Details = string.Empty;
    }

    public BusinessRuleValidationException(string message, string details)
        : base(message)
    {
        Details = details;
    }
}
