using FluentValidation;
using AppValidationException = SchoolAdmission.Application.Common.Exceptions.ValidationException;

namespace SchoolAdmission.Application.Common;

public static class ValidationExtensions
{
    public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        throw new AppValidationException("VALIDATION_FAILED", result.Errors[0].ErrorMessage, errors);
    }
}
