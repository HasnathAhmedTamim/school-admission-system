using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.API.Errors;

/// <summary>
/// Replaces the default ProblemDetails for malformed request bodies (bad JSON, wrong types)
/// with <see cref="ErrorResponse"/>, without leaking .NET type names to the client.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        // Drop the generic "<parameter> field is required" entry that accompanies every body binding failure.
        var parameterNames = context.ActionDescriptor.Parameters
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 } && !parameterNames.Contains(entry.Key))
            .ToDictionary(
                entry => NormalizeKey(entry.Key),
                entry => entry.Value!.Errors
                    .Select(e => e.Exception is not null || e.ErrorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase)
                        ? "The value is not valid."
                        : e.ErrorMessage)
                    .Distinct()
                    .ToArray());

        var body = new ErrorResponse(
            "INVALID_REQUEST",
            "The request body is missing or not valid JSON with the expected fields.",
            errors.Count > 0 ? errors : null);

        return new BadRequestObjectResult(body);
    }

    private static string NormalizeKey(string key)
    {
        var trimmed = key.TrimStart('$', '.');
        return string.IsNullOrEmpty(trimmed) ? "body" : trimmed;
    }
}
