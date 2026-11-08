using System.Text.Json.Serialization;

namespace SchoolAdmission.API.Errors;

/// <summary>Uniform error body returned for every non-2xx response produced by the API.</summary>
public sealed record ErrorResponse(
    string Error,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
