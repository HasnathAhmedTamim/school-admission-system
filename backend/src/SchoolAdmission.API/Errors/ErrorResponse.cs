namespace SchoolAdmission.API.Errors;

/// <summary>Uniform error body returned for every non-2xx response produced by the API.</summary>
public sealed record ErrorResponse(
    string Error,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);
