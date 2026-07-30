using FluentValidation;

namespace SIGH.Application.Validators;

public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
    }
}
